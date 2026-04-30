using AutoMapper;
using RDS.ExpenseTracker.Api.Dtos;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Application.Mappings;

public class ExpenseTrackerProfile : Profile
{
    private static readonly char[] CategoryTagSeparators = [',', ';'];

    public ExpenseTrackerProfile()
    {
        CreateMap<Account, AccountDto>()
            .ReverseMap();

        CreateMap<Transaction, TransactionDto>()
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId ?? 0))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.CategoryNavigation == null ? string.Empty : src.CategoryNavigation.Name))
            .ForMember(dest => dest.AccountId, opt => opt.MapFrom(src => src.AccountId))
            .ForMember(dest => dest.Account, opt => opt.MapFrom(src => src.AccountNavigation.Name));

        CreateMap<TransactionDto, Transaction>()
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId > 0 ? (int?)src.CategoryId : null))
            .ForMember(dest => dest.AccountNavigation, opt => opt.Ignore())
            .ForMember(dest => dest.CategoryNavigation, opt => opt.Ignore())
            .ForMember(dest => dest.TransferNavigation, opt => opt.Ignore());

        CreateMap<Category, CategoryDto>()
            .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority ?? 0))
            .ForMember(dest => dest.IsDefault, opt => opt.MapFrom(src => src.IsDefault ?? false))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description ?? string.Empty))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => SplitCategoryTags(src.Tags)));

        CreateMap<CategoryDto, Category>()
            .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
            .ForMember(dest => dest.IsDefault, opt => opt.MapFrom(src => src.IsDefault))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => JoinCategoryTags(src.Tags)));

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

    private static IEnumerable<string> SplitCategoryTags(string? tags)
        => string.IsNullOrWhiteSpace(tags)
            ? Enumerable.Empty<string>()
            : tags.Split(CategoryTagSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string JoinCategoryTags(IEnumerable<string>? tags)
        => tags is null
            ? string.Empty
            : string.Join(',', tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()));
}
