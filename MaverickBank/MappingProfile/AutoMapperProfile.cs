using AutoMapper;
using MaverickBank.DTOs.Account;
using MaverickBank.DTOs.AuditLog;
using MaverickBank.DTOs.Beneficiary;
using MaverickBank.DTOs.Branch;
using MaverickBank.DTOs.Loan;
using MaverickBank.DTOs.Transaction;
using MaverickBank.DTOs.User;

namespace MaverickBank.MappingProfile
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Models.Branch, BranchResponseDto>();
            CreateMap<CreateBranchDto, Models.Branch>()
                .ForMember(dest => dest.BranchId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            CreateMap<Models.User, UserResponseDto>()
                .ConstructUsing(src => new UserResponseDto(
                    src.UserId,
                    src.RoleId,
                    src.FirstName,
                    src.LastName,
                    src.Email,
                    src.PhoneNumber,
                    src.Gender,
                    src.DateOfBirth,
                    CalculateAge(src.DateOfBirth),
                    src.AadhaarNumber,
                    src.PANNumber,
                    src.AddressLine1,
                    src.AddressLine2,
                    src.City,
                    src.State,
                    src.Pincode,
                    src.IsActive
                ));
            CreateMap<Models.User, UpdateUserDto>();
            CreateMap<CreateUserDto, Models.User>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.RoleId, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
            CreateMap<UpdateUserDto, Models.User>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.RoleId, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.Gender, opt => opt.Ignore())
                .ForMember(dest => dest.DateOfBirth, opt => opt.Ignore())
                .ForMember(dest => dest.AadhaarNumber, opt => opt.Ignore())
                .ForMember(dest => dest.PANNumber, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            CreateMap<CreateAccountDto, Models.Account>()
                .ForMember(dest => dest.AccountId, opt => opt.Ignore())
                .ForMember(dest => dest.AccountNumber, opt => opt.Ignore())
                .ForMember(dest => dest.Balance, opt => opt.MapFrom(src => src.InitialDeposit))
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.OpenedDate, opt => opt.Ignore())
                .ForMember(dest => dest.ClosedDate, opt => opt.Ignore());

            CreateMap<Models.Beneficiary, BeneficiaryResponseDto>();
            CreateMap<AddBeneficiaryDto, Models.Beneficiary>()
                .ForMember(dest => dest.BeneficiaryId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            CreateMap<Models.Transaction, TransactionResponseDto>();

            CreateMap<Models.Loan, LoanResponseDto>()
                .ConstructUsing(src => new LoanResponseDto(
                    src.LoanId,
                    src.LoanApplicationId,
                    src.AccountId,
                    src.ApprovedAmount,
                    src.InterestRate,
                    src.TenureMonths,
                    src.EMIAmount,
                    src.OutstandingAmount,
                    src.StartDate,
                    src.EndDate,
                    src.LoanStatus
                ));
            CreateMap<Models.LoanApplication, LoanResponseDto>()
                .ConstructUsing(src => new LoanResponseDto(
                    0L,
                    src.LoanApplicationId,
                    0L,
                    0m,
                    0m,
                    0,
                    0m,
                    0m,
                    DateTime.MinValue,
                    DateTime.MinValue,
                    src.ApplicationStatus
                ));
            CreateMap<ApplyLoanDto, Models.LoanApplication>()
                .ForMember(dest => dest.LoanApplicationId, opt => opt.Ignore())
                .ForMember(dest => dest.ApplicationStatus, opt => opt.Ignore())
                .ForMember(dest => dest.AppliedDate, opt => opt.Ignore())
                .ForMember(dest => dest.ReviewedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ReviewedDate, opt => opt.Ignore())
                .ForMember(dest => dest.Remarks, opt => opt.Ignore());

            CreateMap<Models.LoanType, LoanTypeResponseDto>();
            CreateMap<CreateLoanTypeDto, Models.LoanType>()
                .ForMember(dest => dest.LoanTypeId, opt => opt.Ignore())
                .ForMember(dest => dest.MinimumTenureMonths, opt => opt.MapFrom(src => src.MinimumTenureMonths));

            CreateMap<Models.AuditLog, AuditLogResponseDto>();
        }
        private static int CalculateAge(DateTime dob)
        {
            var today = DateTime.UtcNow.Date;
            var age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age))
                age--;
            return age;
        }
    }
}
