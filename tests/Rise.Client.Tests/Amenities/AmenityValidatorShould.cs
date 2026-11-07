using Rise.Shared.Amenities;
using Shouldly;

namespace Rise.Client.Amenities;

public class AmenityValidatorShould
{
    private static AmenityRequest.Create ValidCreate() => new()
    {
        Names = new() { ["en"] = "Wi-Fi", ["nl"] = "Wifi" },
        Icon = "fa-wifi",
    };

    [Fact]
    public void AcceptAValidCreateRequest()
    {
        var result = new AmenityRequest.Create.Validator().Validate(ValidCreate());

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RequireAnEnglishName(string? name)
    {
        var request = ValidCreate();
        request.Names["en"] = name;

        var result = new AmenityRequest.Create.Validator().Validate(request);

        result.Errors.ShouldContain(e => e.ErrorMessage == "A name in English is required.");
    }

    [Fact]
    public void AcceptAMissingTranslation()
    {
        var request = ValidCreate();
        request.Names["fr"] = "";

        var result = new AmenityRequest.Create.Validator().Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void RefuseAnUnknownLanguage()
    {
        var request = ValidCreate();
        request.Names["es"] = "Wifi";

        var result = new AmenityRequest.Create.Validator().Validate(request);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void RefuseATooLongName()
    {
        var request = ValidCreate();
        request.Names["de"] = new string('a', 101);

        var result = new AmenityRequest.Create.Validator().Validate(request);

        result.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("wifi")]
    [InlineData("fa-Wifi")]
    [InlineData("fas fa-wifi")]
    public void RefuseAnInvalidIcon(string icon)
    {
        var request = ValidCreate();
        request.Icon = icon;

        var result = new AmenityRequest.Create.Validator().Validate(request);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void RefuseAnEditWithoutId()
    {
        var result = new AmenityRequest.Edit.Validator().Validate(new AmenityRequest.Edit
        {
            Names = new() { ["en"] = "Wi-Fi" },
            Icon = "fa-wifi",
        });

        result.IsValid.ShouldBeFalse();
    }
}
