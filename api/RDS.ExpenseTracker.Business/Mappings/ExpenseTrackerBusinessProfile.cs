using AutoMapper;
using RDS.ExpenseTracker.Domain.Models;
using Entities = RDS.ExpenseTracker.DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.Business.Mappings
{
    public class ExpenseTrackerBusinessProfile : Profile
    {
        public ExpenseTrackerBusinessProfile()
        {
            CreateMap<Entities.Transaction, Transaction>()
                .ForMember(dest => dest.CategoryDescription, opt => opt.MapFrom(src => src.Category.Description));

            CreateMap<Transaction, Entities.Transaction>()
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.FinancialAccount, opt => opt.Ignore());

            CreateMap<Entities.FinancialAccount, FinancialAccount>().ReverseMap();

            CreateMap<Entities.Category, Category>()
                .ForMember(x => x.Tags, opt => opt.MapFrom(src => src.Tags.Split(';', StringSplitOptions.None)))
                .ReverseMap();
        }
    }
}
