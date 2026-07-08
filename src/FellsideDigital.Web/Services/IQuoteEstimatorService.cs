namespace FellsideDigital.Web.Services;

public interface IQuoteEstimatorService
{
    QuoteEstimate Estimate(QuoteSelection selection);
}
