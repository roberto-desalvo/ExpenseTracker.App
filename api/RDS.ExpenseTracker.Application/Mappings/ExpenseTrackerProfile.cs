using AutoMapper;
using RDS.ExpenseTracker.Api.Dtos;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Application.Mappings
{
    public class ExpenseTrackerProfile : Profile
    {
        public ExpenseTrackerProfile()
        {
            CreateMap<Account, FinancialAccountDto>()
                .ReverseMap();

            CreateMap<Transaction, TransactionDto>()
                .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.CategoryNavigation == null ? null : src.CategoryNavigation.Name))
                .ForMember(dest => dest.AccountId, opt => opt.MapFrom(src => src.AccountId))
                .ForMember(dest => dest.Account, opt => opt.MapFrom(src => src.AccountNavigation.Name))
                .ReverseMap();

            CreateMap<Category, CategoryDto>()
                .ReverseMap();
        }
    }
}
