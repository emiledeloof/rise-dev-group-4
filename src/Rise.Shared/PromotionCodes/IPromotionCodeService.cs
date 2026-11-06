namespace Rise.Shared.PromotionCodes;

public interface IPromotionCodeService
{
    Task<Result<PromotionCodeResponse.Create>> CreateAsync(
        PromotionCodeRequest.Create request,
        CancellationToken ctx = default);
}
