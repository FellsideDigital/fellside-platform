namespace FellsideDigital.Web.Components.Home;

// ─── Record types ─────────────────────────────────────────────────────────────

public record Project(
    string Title,
    string Description,
    string[] Tags,
    string MockupImage,
    string Link);

public record Service(
    string Title,
    string Description,
    string IconSvg);

public record Testimonial(
    string Quote,
    string Name,
    string Role,
    string AvatarUrl);

public record Faq(
    string Question,
    string Answer);

// ─── Static data ──────────────────────────────────────────────────────────────

public static class HomeData
{
    public static readonly IReadOnlyList<Project> Projects = new[]
    {
        new Project(
            "Client Portal",
            "A bespoke project-tracking portal giving clients real-time visibility into milestones, tasks, and updates.",
            new[] { "Blazor", "Real-time", "Auth" },
            "/images/mockup-portal.png",
            "/work/portal"),

        new Project(
            "E-Commerce Platform",
            "A custom storefront with integrated payments, inventory management, and an admin dashboard built from the ground up.",
            new[] { "E-Commerce", "Stripe", "Dashboard" },
            "/images/mockup-ecommerce.png",
            "/work/ecommerce"),

        new Project(
            "Booking & Scheduling System",
            "Automated booking with calendar sync, Stripe payments, and email/SMS notifications for a hospitality business.",
            new[] { "Bookings", "Payments", "Notifications" },
            "/images/mockup-booking.png",
            "/work/booking"),

        new Project(
            "Internal Operations Dashboard",
            "A data-rich internal tool connecting multiple APIs and surfacing live KPIs and alerts for a growing SME.",
            new[] { "Dashboard", "APIs", "Reporting" },
            "/images/mockup-dashboard.png",
            "/work/dashboard"),
    };

    public static readonly List<Service> Services =
    [
        new(
        Title: "Custom Website Design",
        Description: "Beautifully crafted, high-performance websites designed to reflect your brand and convert visitors. No templates — every pixel is built for you.",
        IconSvg: """
            <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none"
                 viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
              <path stroke-linecap="round" stroke-linejoin="round"
                    d="M9 17.25v1.007a3 3 0 01-.879 2.122L7.5 21h9l-.621-.621A3 3 0 0115 18.257V17.25m6-12V15a2.25 2.25 0 01-2.25 2.25H5.25A2.25 2.25 0 013 15V5.25m18 0A2.25 2.25 0 0018.75 3H5.25A2.25 2.25 0 003 5.25m18 0H3"/>
            </svg>
        """
    ),
    new(
        Title: "E-Commerce & Web Applications",
        Description: "Full-featured web applications and online stores built from the ground up — custom portals, dashboards, booking systems, and more.",
        IconSvg: """
            <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none"
                 viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
              <path stroke-linecap="round" stroke-linejoin="round"
                    d="M2.25 3h1.386c.51 0 .955.343 1.087.835l.383 1.437M7.5 14.25a3 3 0 00-3 3h15.75m-12.75-3h11.218c1.121-2.3 2.1-4.684 2.924-7.138a60.114 60.114 0 00-16.536-1.84M7.5 14.25L5.106 5.272M6 20.25a.75.75 0 11-1.5 0 .75.75 0 011.5 0zm12.75 0a.75.75 0 11-1.5 0 .75.75 0 011.5 0z"/>
            </svg>
        """
    ),
    new(
        Title: "Workflow Automation",
        Description: "Map, streamline, and automate your repetitive internal processes — from approvals and notifications to task handoffs and status updates.",
        IconSvg: """
            <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none"
                 viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
              <path stroke-linecap="round" stroke-linejoin="round"
                    d="M3.75 13.5l10.5-11.25L12 10.5h8.25L9.75 21.75 12 13.5H3.75z"/>
            </svg>
        """
    ),
    new(
        Title: "API & Systems Integration",
        Description: "Break down data silos by connecting your existing tools — CRMs, ERPs, databases, and third-party platforms — into a single, unified workflow.",
        IconSvg: """
            <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none"
                 viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
              <path stroke-linecap="round" stroke-linejoin="round"
                    d="M13.19 8.688a4.5 4.5 0 011.242 7.244l-4.5 4.5a4.5 4.5 0 01-6.364-6.364l1.757-1.757m13.35-.622l1.757-1.757a4.5 4.5 0 00-6.364-6.364l-4.5 4.5a4.5 4.5 0 001.242 7.244"/>
            </svg>
        """
    ),
    new(
        Title: "AI-Powered Automation",
        Description: "Go beyond rules-based automation. We embed AI into your workflows to handle unstructured data, intelligent decisions, and dynamic task routing.",
        IconSvg: """
            <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none"
                 viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
              <path stroke-linecap="round" stroke-linejoin="round"
                    d="M9.813 15.904L9 18.75l-.813-2.846a4.5 4.5 0 00-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 003.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 003.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 00-3.09 3.09z"/>
            </svg>
        """
    ),
    new(
        Title: "Data & Reporting Automation",
        Description: "Eliminate manual reporting. Automatically collect, transform, and deliver accurate data insights to the right people at the right time.",
        IconSvg: """
            <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none"
                 viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
              <path stroke-linecap="round" stroke-linejoin="round"
                    d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 013 19.875v-6.75zM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V8.625zM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V4.125z"/>
            </svg>
        """
    ),
    new(
        Title: "Process Discovery & Consultancy",
        Description: "Not sure where to start? We audit your operations, identify automation opportunities, and deliver a clear, prioritised roadmap for implementation.",
        IconSvg: """
            <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none"
                 viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
              <path stroke-linecap="round" stroke-linejoin="round"
                    d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m5.231 13.481L15 17.25m-4.5-15H5.625c-.621 0-1.125.504-1.125 1.125v16.5c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z"/>
            </svg>
        """
    ),
    new(
        Title: "Legacy System Modernisation",
        Description: "Bring ageing infrastructure into the modern era. We integrate, extend, or replace legacy systems without disrupting your day-to-day operations.",
        IconSvg: """
            <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none"
                 viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
              <path stroke-linecap="round" stroke-linejoin="round"
                    d="M21 7.5l-9-5.25L3 7.5m18 0l-9 5.25m9-5.25v9l-9 5.25M3 7.5l9 5.25M3 7.5v9l9 5.25m0-9v9"/>
            </svg>
        """
    ),
    new(
        Title: "Scheduled & Event-Driven Automation",
        Description: "Run automations on a schedule or trigger them from real-world events — form submissions, payments, status changes, or inbound data from any source.",
        IconSvg: """
            <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none"
                 viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
              <path stroke-linecap="round" stroke-linejoin="round"
                    d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z"/>
            </svg>
        """
    ),
    new(
        Title: "Ongoing Support & Optimisation",
        Description: "Automation isn't a one-and-done project. We monitor, maintain, and continuously improve your automations as your business evolves.",
        IconSvg: """
            <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none"
                 viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.75">
              <path stroke-linecap="round" stroke-linejoin="round"
                    d="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0l3.181 3.183a8.25 8.25 0 0013.803-3.7M4.031 9.865a8.25 8.25 0 0113.803-3.7l3.181 3.182m0-4.991v4.99"/>
            </svg>
        """
    ),
];

    public static readonly IReadOnlyList<Testimonial> Testimonials = new[]
    {
        new Testimonial(
            "The portal Fellside Digital built has completely changed how we communicate progress. Clients love having real-time visibility into their projects.",
            "Jane Cooper", "Director, Lakeland Consulting", ""),

        new Testimonial(
            "Professional, communicative, and technically excellent. The booking system is rock-solid and has saved us hours every single week.",
            "Tom Wright", "Owner, Eden Valley Stays", ""),

        new Testimonial(
            "We came to Fellside Digital with a complex brief and they delivered something beyond what we imagined. Clear design, fast, built to last.",
            "Sarah Hodgson", "Operations Manager, Northern Roots CIC", ""),
    };

    public static readonly IReadOnlyList<Faq> Faqs = new[]
    {
        new Faq(
            "Why choose Fellside Digital?",
            "You deal directly with the developers doing the work, no account managers, no hand-offs. We keep overhead low, which means better value and a more personal service throughout."),

        new Faq(
            "What kind of projects do you take on?",
            "We build premium websites, custom web applications, e-commerce platforms, client portals, internal dashboards, and intelligent automation systems. We work with SMEs, agencies, and non-profits across the UK."),

        new Faq(
            "What technology stack do you use?",
            "We build primarily with Blazor and .NET 9, with Tailwind CSS for UI. For databases we typically use SQL Server or PostgreSQL. We deploy to Azure, AWS, or managed VPS depending on your needs."),

        new Faq(
            "How much does a project cost?",
            "A foundational website starts from around £450. Software-driven sites and bespoke systems are scoped individually. We always provide a clear, fixed quote before any work begins."),

        new Faq(
            "How long does a project take?",
            "Simpler sites can be delivered in 2–4 weeks. Complex software systems typically take 2–4 months. We agree a timeline upfront and keep you updated via your dedicated project portal."),

        new Faq(
            "Can I track progress during the build?",
            "Yes, every client gets access to our project portal with live milestones, task updates, and feedback threads. No more chasing emails for updates."),

        new Faq(
            "Do you offer ongoing support after launch?",
            "Absolutely. We offer retainer-based maintenance packages covering hosting, updates, backups, monitoring, and priority support. We build software to last."),

        new Faq(
            "What do I need to get started?",
            "It helps to know: what is the main purpose of the software, who are the users, and do you have any brand guidelines or designs? If you're unsure about any of it, just get in touch and we'll work through it together."),
    };
}
