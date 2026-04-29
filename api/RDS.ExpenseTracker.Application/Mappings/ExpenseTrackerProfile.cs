using AutoMapper;
using RDS.ExpenseTracker.Api.Dtos;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Application.Mappings;

public class ExpenseTrackerProfile : Profile
{
    public ExpenseTrackerProfile()
    {
        CreateMap<Account, AccountDto>()
            .ReverseMap();

        CreateMap<Transaction, TransactionDto>()
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.CategoryNavigation == null ? null : src.CategoryNavigation.Name))
            .ForMember(dest => dest.AccountId, opt => opt.MapFrom(src => src.AccountId))
            .ForMember(dest => dest.Account, opt => opt.MapFrom(src => src.AccountNavigation.Name))
            .ReverseMap();

        CreateMap<Category, CategoryDto>()
            .ReverseMap();

        CreateMap<Transfer, TransferDto>()
            .ForMember(dest => dest.FromAccountId,
                opt => opt.MapFrom(src => src.Transactions
                    .Where(t => t.Amount < 0m)
                    .Select(t => t.AccountId)
                    .FirstOrDefault()))
            .ForMember(dest => dest.ToAccountId,
                opt => opt.MapFrom(src => src.Transactions
                    .Where(t => t.Amount > 0m)
                    .Select(t => t.AccountId)
                    .FirstOrDefault()))
            .ForMember(dest => dest.Amount,
                opt => opt.MapFrom(src => Math.Abs(src.Transactions
                    .Where(t => t.Amount < 0m)
                    .Select(t => t.Amount)
                    .FirstOrDefault())))
            .ForMember(dest => dest.Description,
                opt => opt.MapFrom(src => src.Transactions
                    .Select(t => t.Description)
                    .FirstOrDefault() ?? string.Empty))
            .ForMember(dest => dest.Date,
                opt => opt.MapFrom(src => src.Transactions
                    .Select(t => t.Date)
                    .FirstOrDefault()));
    }
}
