namespace EarthRelief3D.Core.Model;

/// <summary>
/// One humanitarian appeal (plan) of a country for a year, from OCHA's Financial Tracking Service (Plan 4, ruling P4-4):
/// what it asks for and what has been given, as FTS publishes them. A <see cref="Regional"/> plan's figures are for the whole
/// plan, all its countries; they are never a country's own figure.
/// </summary>
/// <param name="RequirementsUsd">Null when the file's cell is blank (never 0).</param>
/// <param name="FundingUsd">Funding reported against the plan; a blank cell reads 0 (nothing reported).</param>
/// <param name="PercentFunded">FTS's own whole percent, as published; null when blank.</param>
/// <param name="Regional">The plan id is listed under more than one country, or its type says "Regional".</param>
public sealed record AppealFunding(string Iso3, int PlanId, string PlanName, string PlanType, int Year, long? RequirementsUsd,
    long FundingUsd, int? PercentFunded, bool Regional, SourceCitation Citation);
