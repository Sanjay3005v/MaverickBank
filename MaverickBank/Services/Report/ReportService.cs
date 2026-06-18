using MaverickBank.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MaverickBank.Services.Report
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _context;

        public ReportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> GenerateTransactionReportAsync(long accountId, DateTime? from, DateTime? to)
        {
            var account = await _context.Accounts
                .Where(a => a.AccountId == accountId)
                .Join(_context.Branches, a => a.BranchId, b => b.BranchId, (a, b) => new { a, b })
                .Join(_context.AccountTypes, x => x.a.AccountTypeId, t => t.AccountTypeId, (x, t) => new
                {
                    x.a.AccountId,
                    x.a.AccountNumber,
                    x.a.Balance,
                    x.a.Status,
                    x.a.OpenedDate,
                    x.a.UserId,
                    BranchName = x.b.BranchName,
                    AccountTypeName = t.TypeName
                })
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException($"Account with ID {accountId} not found.");

            var user = await _context.Users.FindAsync(account.UserId)
                ?? throw new KeyNotFoundException("User not found.");

            var query = _context.Transactions
                .Where(t => t.FromAccountId == accountId || t.ToAccountId == accountId)
                .Join(_context.TransactionTypes, t => t.TransactionTypeId, tt => tt.TransactionTypeId,
                    (t, tt) => new
                    {
                        t.TransactionId,
                        t.TransactionDate,
                        t.Amount,
                        t.TransactionReference,
                        t.Description,
                        t.TransactionStatus,
                        t.FromAccountId,
                        t.ToAccountId,
                        TypeName = tt.TypeName
                    })
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(t => t.TransactionDate >= from.Value);
            if (to.HasValue)
                query = query.Where(t => t.TransactionDate <= to.Value);

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            var totalInbound = transactions
                .Where(t => t.ToAccountId == accountId)
                .Sum(t => t.Amount);

            var totalOutbound = transactions
                .Where(t => t.FromAccountId == accountId)
                .Sum(t => t.Amount);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(inner =>
                            {
                                inner.Item().Text("MaverickBank")
                                    .FontSize(20).Bold().FontColor("#1a237e");
                                inner.Item().Text("Account Transaction Report")
                                    .FontSize(11).FontColor("#555555");
                            });
                            row.ConstantItem(160).AlignRight().Column(inner =>
                            {
                                inner.Item().Text($"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC")
                                    .FontSize(8).FontColor("#888888");
                                if (from.HasValue || to.HasValue)
                                {
                                    var range = $"{(from.HasValue ? from.Value.ToString("dd MMM yyyy") : "Start")} – {(to.HasValue ? to.Value.ToString("dd MMM yyyy") : "Today")}";
                                    inner.Item().Text($"Period: {range}")
                                        .FontSize(8).FontColor("#888888");
                                }
                            });
                        });

                        col.Item().PaddingTop(8).BorderBottom(1).BorderColor("#1a237e");
                        col.Item().PaddingTop(10);

                        col.Item().Background("#f5f5f5").Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(inner =>
                            {
                                inner.Item().Text($"Account Holder").FontSize(8).FontColor("#888888");
                                inner.Item().Text($"{user.FirstName} {user.LastName}").Bold();
                                inner.Item().PaddingTop(4).Text($"Account Number").FontSize(8).FontColor("#888888");
                                inner.Item().Text(account.AccountNumber).Bold();
                            });
                            row.RelativeItem().Column(inner =>
                            {
                                inner.Item().Text("Account Type").FontSize(8).FontColor("#888888");
                                inner.Item().Text(account.AccountTypeName).Bold();
                                inner.Item().PaddingTop(4).Text("Branch").FontSize(8).FontColor("#888888");
                                inner.Item().Text(account.BranchName).Bold();
                            });
                            row.RelativeItem().Column(inner =>
                            {
                                inner.Item().Text("Status").FontSize(8).FontColor("#888888");
                                inner.Item().Text(account.Status).Bold();
                                inner.Item().PaddingTop(4).Text("Current Balance").FontSize(8).FontColor("#888888");
                                inner.Item().Text($"₹ {account.Balance:N2}").Bold().FontColor("#1a237e");
                            });
                        });

                        col.Item().PaddingTop(12);
                    });

                    page.Content().Column(col =>
                    {

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Border(1).BorderColor("#e0e0e0").Padding(8).Column(inner =>
                            {
                                inner.Item().Text("Total Inbound").FontSize(8).FontColor("#888888");
                                inner.Item().Text($"₹ {totalInbound:N2}").Bold().FontColor("#2e7d32");
                            });
                            row.ConstantItem(8);
                            row.RelativeItem().Border(1).BorderColor("#e0e0e0").Padding(8).Column(inner =>
                            {
                                inner.Item().Text("Total Outbound").FontSize(8).FontColor("#888888");
                                inner.Item().Text($"₹ {totalOutbound:N2}").Bold().FontColor("#c62828");
                            });
                            row.ConstantItem(8);
                            row.RelativeItem().Border(1).BorderColor("#e0e0e0").Padding(8).Column(inner =>
                            {
                                inner.Item().Text("Total Transactions").FontSize(8).FontColor("#888888");
                                inner.Item().Text($"{transactions.Count}").Bold();
                            });
                        });

                        col.Item().PaddingTop(12);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(70);
                                cols.RelativeColumn(2);
                                cols.ConstantColumn(55);
                                cols.RelativeColumn(3);
                                cols.ConstantColumn(75);
                                cols.ConstantColumn(45);
                            });

                            static IContainer HeaderCell(IContainer c) =>
                                c.Background("#1a237e").Padding(5).AlignMiddle();

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("Date").FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Element(HeaderCell).Text("Reference").FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Element(HeaderCell).Text("Type").FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Element(HeaderCell).Text("Description").FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Element(HeaderCell).Text("Amount (₹)").FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Element(HeaderCell).Text("Status").FontColor(Colors.White).Bold().FontSize(8);
                            });

                            for (int i = 0; i < transactions.Count; i++)
                            {
                                var tx = transactions[i];
                                var isInbound = tx.ToAccountId == accountId;
                                var bg = "#fafafa";

                                IContainer Cell(IContainer c) =>
                                    c.Background(bg).BorderBottom(1).BorderColor("#eeeeee").Padding(5).AlignMiddle();

                                table.Cell().Element(Cell).Text(tx.TransactionDate.ToString("dd MMM yyyy")).FontSize(8);
                                table.Cell().Element(Cell).Text(tx.TransactionReference).FontSize(7).FontColor("#555555");
                                table.Cell().Element(Cell).Text(tx.TypeName).FontSize(8);
                                table.Cell().Element(Cell).Text(tx.Description ?? "-").FontSize(8).FontColor("#555555");
                                table.Cell().Element(Cell)
                                    .Text($"{(isInbound ? "+" : "-")} {tx.Amount:N2}")
                                    .FontSize(8).Bold()
                                    .FontColor(isInbound ? "#2e7d32" : "#c62828");
                                table.Cell().Element(Cell).Text(tx.TransactionStatus).FontSize(8);
                            }

                            if (transactions.Count == 0)
                            {
                                table.Cell().ColumnSpan(6).Padding(20)
                                    .AlignCenter().Text("No transactions found for the selected period.")
                                    .FontColor("#888888").Italic();
                            }
                        });
                    });

                    page.Footer().BorderTop(1).BorderColor("#e0e0e0").PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().Text("MaverickBank — Confidential")
                            .FontSize(7).FontColor("#aaaaaa");
                        row.ConstantItem(100).AlignRight()
                            .Text(x =>
                            {
                                x.Span("Page ").FontSize(7).FontColor("#aaaaaa");
                                x.CurrentPageNumber().FontSize(7).FontColor("#aaaaaa");
                                x.Span(" of ").FontSize(7).FontColor("#aaaaaa");
                                x.TotalPages().FontSize(7).FontColor("#aaaaaa");
                            });
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
