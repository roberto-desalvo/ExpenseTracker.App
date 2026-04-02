using AutoMapper;
using RDS.ExpenseTracker.Api.Dtos;
using RDS.ExpenseTracker.Domain.Models;

namespace RDS.ExpenseTracker.DataImport.Business.Mappings
{
    public class ExpenseTrackerBusinessProfile : Profile
    {
        public ExpenseTrackerBusinessProfile()
        {
            CreateMap<FinancialAccountDto, FinancialAccount>().ReverseMap();
            CreateMap<CategoryDto, Category>().ReverseMap();
            CreateMap<TransactionDto, Transaction>()
                .ForMember(x => x.CategoryDescription, opt => opt.MapFrom(x => x.Category))
                .ForMember(x => x.FinancialAccountName, opt => opt.MapFrom(x => x.Account))
                .ForMember(x => x.FinancialAccountId, opt => opt.MapFrom(x => x.AccountId))
                .ReverseMap();
        }
    }
}
