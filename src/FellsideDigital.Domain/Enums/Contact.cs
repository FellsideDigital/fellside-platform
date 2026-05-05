using System.ComponentModel.DataAnnotations;

namespace FellsideDigital.Domain.Enums;

public enum ContactServiceType
{
    [Display(Name = "Business Website")]
    BusinessWebsite,

    [Display(Name = "Website Redesign")]
    WebsiteRedesign,

    [Display(Name = "Landing Page / Funnel")]
    LandingPage,

    [Display(Name = "E-Commerce Website")]
    ECommerceWebsite,

    [Display(Name = "Booking & Scheduling System")]
    BookingSystem,

    [Display(Name = "Business Process Automation")]
    BusinessAutomation,

    [Display(Name = "System Integration (Connecting Tools)")]
    SystemIntegration,

    [Display(Name = "Ongoing Support & Maintenance")]
    SupportMaintenance,

    [Display(Name = "Something else")]
    SomethingElse
}

public enum ContactBudget
{
    [Display(Name = "Under £2,000")]
    Under2k,

    [Display(Name = "£2,000 – £5,000")]
    TwoToFiveK,

    [Display(Name = "£5,000 – £10,000")]
    FiveToTenK,

    [Display(Name = "£10,000 – £25,000")]
    TenToTwentyFiveK,

    [Display(Name = "£25,000+")]
    Over25k,

    [Display(Name = "Not sure yet")]
    NotSure
}

public enum ContactHowHeard
{
    [Display(Name = "Google search")]
    GoogleSearch,

    [Display(Name = "Social media")]
    SocialMedia,

    [Display(Name = "Word of mouth")]
    WordOfMouth,

    [Display(Name = "Returning client")]
    ReturningClient,

    Other
}
