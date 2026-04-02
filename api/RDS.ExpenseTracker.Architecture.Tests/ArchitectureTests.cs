using ArchUnitNET.Loader;
using RDS.ExpenseTracker.Api.Architecture;
using RDS.ExpenseTracker.Business.Architecture;
using RDS.ExpenseTracker.DataAccess.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ArchUnitNET.Fluent;
using ArchUnitNET.Domain;
using System.Text.RegularExpressions;
using FluentAssertions;
using RDS.ExpenseTracker.Domain.Architecture;
using ArchUnitNET.xUnitV3;


namespace RDS.ExpenseTracker.ArchitectureTests
{
    public class ArchitectureTests
    {
        private readonly IObjectProvider<IType> PresentationLayer =
            Types().That().ResideInAssembly($@"^{Regex.Escape(typeof(PresentationAssemblyMarker).Assembly.GetName().Name)}(\..*)?$")
            .As("Presentation Layer");

        private readonly IObjectProvider<IType> DataAccessLayer =
            Types().That().ResideInAssembly($@"^{Regex.Escape(typeof(DataAccessAssemblyMarker).Assembly.GetName().Name)}(\..*)?$")
            .As("DataAccess Layer");

        private readonly IObjectProvider<IType> BusinessLayer =
            Types().That().ResideInAssembly($@"^{Regex.Escape(typeof(BusinessAssemblyMarker).Assembly.GetName().Name)}(\..*)?$")
            .As("Business Layer");


        private static readonly Architecture Architecture = new ArchLoader()
            .LoadAssemblies(
                typeof(PresentationAssemblyMarker).Assembly,
                typeof(BusinessAssemblyMarker).Assembly,
                typeof(DataAccessAssemblyMarker).Assembly,
                typeof(DomainAssemblyMarker).Assembly)
            .Build();

        [Fact]
        public void PresentationLayer_ShouldNotDependOnDataLayer()
        {
            IArchRule rule = Types()
                .That().Are(PresentationLayer)
                .Should().NotDependOnAny(DataAccessLayer)
                .Because("it's forbidden")
                .WithoutRequiringPositiveResults();

            rule.Check(Architecture);
        }

        [Theory]
        [InlineData(typeof(PresentationAssemblyMarker))]
        [InlineData(typeof(DataAccessAssemblyMarker))]
        [InlineData(typeof(BusinessAssemblyMarker))]
        [InlineData(typeof(DomainAssemblyMarker))]
        public void AllAssemblyClasses_ShouldResideInProperNamespace(System.Type type)
        {
            var assembly = type.Assembly;
            var expectedNamespacePrefix = assembly.GetName().Name;
            var namespaceRegexPattern = $@"^{Regex.Escape(expectedNamespacePrefix)}(\..*)?$";

            var rule = Types()
                .That().ResideInAssembly(assembly)
                .And().DoNotHaveName("Program")
                .Should().ResideInNamespace(namespaceRegexPattern, useRegularExpressions: true);

            rule.Check(Architecture);
        }
    }
}
