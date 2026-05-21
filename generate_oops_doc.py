from docx import Document
from docx.shared import Pt, RGBColor, Inches, Cm
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_ALIGN_VERTICAL
from docx.oxml.ns import qn
from docx.oxml import OxmlElement
import copy

doc = Document()

# ── Page margins ──────────────────────────────────────────────────────────────
for section in doc.sections:
    section.top_margin    = Cm(2.0)
    section.bottom_margin = Cm(2.0)
    section.left_margin   = Cm(2.5)
    section.right_margin  = Cm(2.5)

# ── Helpers ───────────────────────────────────────────────────────────────────
RED    = RGBColor(0xC0, 0x25, 0x25)
DARK   = RGBColor(0x1A, 0x1A, 0x2E)
BLUE   = RGBColor(0x15, 0x57, 0x99)
TEAL   = RGBColor(0x00, 0x7A, 0x6E)
GREY   = RGBColor(0x55, 0x55, 0x55)
LGREY  = RGBColor(0xF2, 0xF2, 0xF2)

def shade_cell(cell, hex_fill):
    tc   = cell._tc
    tcPr = tc.get_or_add_tcPr()
    shd  = OxmlElement("w:shd")
    shd.set(qn("w:val"),   "clear")
    shd.set(qn("w:color"), "auto")
    shd.set(qn("w:fill"),  hex_fill)
    tcPr.append(shd)

def set_cell_border(cell, **kwargs):
    tc   = cell._tc
    tcPr = tc.get_or_add_tcPr()
    tcBorders = OxmlElement("w:tcBorders")
    for side in ("top","left","bottom","right","insideH","insideV"):
        val = kwargs.get(side, {})
        if val:
            tag = OxmlElement(f"w:{side}")
            tag.set(qn("w:val"),   val.get("val",   "single"))
            tag.set(qn("w:sz"),    val.get("sz",    "4"))
            tag.set(qn("w:space"), val.get("space", "0"))
            tag.set(qn("w:color"), val.get("color", "auto"))
            tcBorders.append(tag)
    tcPr.append(tcBorders)

def heading(text, level=1, color=DARK, space_before=14, space_after=6):
    p  = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(space_before)
    p.paragraph_format.space_after  = Pt(space_after)
    run = p.add_run(text)
    run.bold      = True
    run.font.size = Pt({1:20, 2:15, 3:13}.get(level, 13))
    run.font.color.rgb = color
    return p

def subheading(text, color=BLUE):
    return heading(text, level=2, color=color, space_before=10, space_after=4)

def h3(text, color=TEAL):
    return heading(text, level=3, color=color, space_before=8, space_after=3)

def body(text, bold_parts=None):
    """Add a normal paragraph; bold_parts is list of substrings to bold."""
    if bold_parts is None:
        p = doc.add_paragraph(text)
    else:
        p = doc.add_paragraph()
        remaining = text
        for bp in bold_parts:
            idx = remaining.find(bp)
            if idx == -1:
                continue
            p.add_run(remaining[:idx])
            r = p.add_run(bp)
            r.bold = True
            remaining = remaining[idx + len(bp):]
        p.add_run(remaining)
    p.paragraph_format.space_after = Pt(4)
    for run in p.runs:
        run.font.size  = Pt(11)
        run.font.color.rgb = DARK
    return p

def bullet(text, indent=1):
    p = doc.add_paragraph(style="List Bullet")
    p.paragraph_format.left_indent   = Inches(0.3 * indent)
    p.paragraph_format.space_after   = Pt(3)
    run = p.add_run(text)
    run.font.size = Pt(11)
    run.font.color.rgb = DARK
    return p

def code_block(lines):
    """Render lines as a light-grey code block."""
    for line in lines:
        p   = doc.add_paragraph()
        p.paragraph_format.space_before = Pt(0)
        p.paragraph_format.space_after  = Pt(0)
        p.paragraph_format.left_indent  = Inches(0.3)
        run = p.add_run(line)
        run.font.name   = "Courier New"
        run.font.size   = Pt(9.5)
        run.font.color.rgb = RGBColor(0x1E, 0x1E, 0x1E)
        # grey paragraph background via paragraph shading
        pPr  = p._p.get_or_add_pPr()
        shd  = OxmlElement("w:shd")
        shd.set(qn("w:val"),   "clear")
        shd.set(qn("w:color"), "auto")
        shd.set(qn("w:fill"),  "F0F0F0")
        pPr.append(shd)

def info_box(label, text, label_color="1A5799", box_color="EBF5FB"):
    table = doc.add_table(rows=1, cols=1)
    table.style = "Table Grid"
    cell  = table.cell(0, 0)
    shade_cell(cell, box_color)
    set_cell_border(cell,
        top    = {"val":"single","sz":"4","color":"1A5799"},
        left   = {"val":"single","sz":"12","color":"1A5799"},
        bottom = {"val":"single","sz":"4","color":"1A5799"},
        right  = {"val":"single","sz":"4","color":"1A5799"},
    )
    p  = cell.paragraphs[0]
    r1 = p.add_run(label + "  ")
    r1.bold = True
    r1.font.color.rgb = RGBColor(0x1A, 0x57, 0x99)
    r1.font.size = Pt(10.5)
    r2 = p.add_run(text)
    r2.font.size = Pt(10.5)
    r2.font.color.rgb = DARK
    p.paragraph_format.space_before = Pt(4)
    p.paragraph_format.space_after  = Pt(4)
    doc.add_paragraph()   # spacer

def divider():
    p   = doc.add_paragraph()
    pPr = p._p.get_or_add_pPr()
    pBdr= OxmlElement("w:pBdr")
    bot = OxmlElement("w:bottom")
    bot.set(qn("w:val"),   "single")
    bot.set(qn("w:sz"),    "6")
    bot.set(qn("w:space"), "1")
    bot.set(qn("w:color"), "C02525")
    pBdr.append(bot)
    pPr.append(pBdr)
    p.paragraph_format.space_after = Pt(6)

# ═══════════════════════════════════════════════════════════════════════════════
#  COVER PAGE
# ═══════════════════════════════════════════════════════════════════════════════
cover = doc.add_paragraph()
cover.paragraph_format.space_before = Pt(40)
cover.paragraph_format.space_after  = Pt(4)
cover.alignment = WD_ALIGN_PARAGRAPH.CENTER
r = cover.add_run("OOPS IN PROJECT")
r.bold = True
r.font.size = Pt(30)
r.font.color.rgb = RED

sub = doc.add_paragraph()
sub.alignment = WD_ALIGN_PARAGRAPH.CENTER
sub.paragraph_format.space_after = Pt(6)
rs = sub.add_run("Bus Booking System — Full Stack Application")
rs.font.size = Pt(16)
rs.font.color.rgb = DARK
rs.bold = True

tech = doc.add_paragraph()
tech.alignment = WD_ALIGN_PARAGRAPH.CENTER
tech.paragraph_format.space_after = Pt(30)
rt = tech.add_run("Angular  ·  .NET Web API  ·  PostgreSQL")
rt.font.size = Pt(12)
rt.font.color.rgb = GREY
rt.italic = True

divider()

task = doc.add_paragraph()
task.alignment = WD_ALIGN_PARAGRAPH.CENTER
task.paragraph_format.space_before = Pt(16)
task.paragraph_format.space_after  = Pt(4)
rt2 = task.add_run("Assignment Task")
rt2.bold = True
rt2.font.size = Pt(13)
rt2.font.color.rgb = BLUE

desc = doc.add_paragraph()
desc.alignment = WD_ALIGN_PARAGRAPH.CENTER
desc.paragraph_format.space_after = Pt(40)
rd = desc.add_run(
    "Create a document that explains your project based on what we learned in OOPS today.\n"
    "If the concepts are already done — display them and explain what you understand better now.\n"
    "If not done already — add those points and explain how it makes it better."
)
rd.font.size = Pt(11)
rd.font.color.rgb = GREY
rd.italic = True

doc.add_page_break()

# ═══════════════════════════════════════════════════════════════════════════════
#  SECTION 1 — PROJECT OVERVIEW
# ═══════════════════════════════════════════════════════════════════════════════
heading("1. Project Overview", 1, RED)
divider()

body(
    "The Bus Booking System is a full-stack web application that replicates real-world "
    "inter-city bus travel booking workflows. It is built using Angular (frontend), "
    ".NET 10 Web API (backend), and PostgreSQL (database).",
    bold_parts=["Angular", ".NET 10 Web API", "PostgreSQL"]
)
body(
    "The system supports three user roles — Customer, Operator, and Admin — each with "
    "dedicated workflows and dashboards.",
    bold_parts=["Customer", "Operator", "Admin"]
)

subheading("1.1 Folder Structure")
body("The backend is cleanly organised into dedicated folders, each with a single responsibility:")

folders = [
    ("Backend/Models/",      "C# entity classes that map to PostgreSQL tables (Booking, Bus, User, Feedback, …)"),
    ("Backend/Controllers/", "ASP.NET controllers that expose REST API endpoints"),
    ("Backend/Services/",    "Business-logic classes (EmailService) — separated from controllers"),
    ("Backend/Interfaces/",  "C# interfaces that define service contracts (IEmailService)  ← NEW"),
    ("Backend/Data/",        "Entity Framework AppDbContext — database session / DbSet definitions"),
    ("Frontend/src/app/pages/",     "Angular page components (Home, Login, Seat, Payment, Ticket, …)"),
    ("Frontend/src/app/components/","Shared UI components (Navbar)"),
    ("Frontend/src/app/services/",  "Angular services that call the backend API"),
]

table = doc.add_table(rows=1, cols=2)
table.style = "Table Grid"
hdr = table.rows[0].cells
hdr[0].text = "Folder"
hdr[1].text = "Purpose"
for c in hdr:
    shade_cell(c, "C02525")
    for p in c.paragraphs:
        for r in p.runs:
            r.bold = True
            r.font.color.rgb = RGBColor(0xFF, 0xFF, 0xFF)
            r.font.size = Pt(10.5)

for folder, purpose in folders:
    row = table.add_row().cells
    row[0].text = folder
    row[1].text = purpose
    shade_cell(row[0], "FFF8F8")
    for cell in row:
        for p in cell.paragraphs:
            for r in p.runs:
                r.font.size = Pt(10)
doc.add_paragraph()

doc.add_page_break()

# ═══════════════════════════════════════════════════════════════════════════════
#  SECTION 2 — CONCEPTS LEARNED TODAY
# ═══════════════════════════════════════════════════════════════════════════════
heading("2. OOP Concepts Learned Today", 1, RED)
divider()
body("The following core OOP concepts were covered in today's session:")
concepts = [
    "Encapsulation — grouping related data and behaviour inside a class, controlling access via properties",
    "Abstraction — hiding internal complexity; exposing only what the caller needs",
    "Inheritance — deriving classes to reuse and extend existing behaviour",
    "Polymorphism — Method Overloading (same name, different parameters) and Method Overriding (redefining inherited methods)",
    "Interfaces — defining contracts that service classes must implement (loose coupling, easy testing)",
    "Coding Standards — switch statements, short single-purpose methods, clean folder organisation",
]
for c in concepts:
    bullet(c)

doc.add_page_break()

# ═══════════════════════════════════════════════════════════════════════════════
#  SECTION 3 — ENCAPSULATION
# ═══════════════════════════════════════════════════════════════════════════════
heading("3. Encapsulation", 1, RED)
divider()

info_box("Status:", "Already present in the project — further reinforced with understanding")

subheading("3.1 What is Encapsulation?")
body(
    "Encapsulation means bundling data (fields) and behaviour (methods) that belong together "
    "inside a single class, and controlling how the outside world reads or writes that data "
    "using access modifiers and C# properties."
)

subheading("3.2 Where is it used?  —  Backend/Models/Booking.cs")
body(
    "Every model class in the Models/ folder applies encapsulation. Booking.cs is the clearest "
    "example — it groups every piece of data that belongs to a seat booking into one self-contained class."
)

code_block([
    "// File: Backend/Models/Booking.cs",
    "[Table(\"bookings\")]",
    "public class Booking",
    "{",
    "    [Column(\"id\")]          public int     Id            { get; set; }",
    "    [Column(\"user_id\")]     public int     UserId        { get; set; }",
    "    [Column(\"bus_id\")]      public int     BusId         { get; set; }",
    "    [Column(\"seat_number\")] public int     SeatNumber    { get; set; }",
    "    [Column(\"status\")]      public string  Status        { get; set; } = \"Booked\";",
    "    [Column(\"lock_expiry\")] public DateTime? LockExpiry  { get; set; }",
    "    [Column(\"payment_status\")] public string PaymentStatus { get; set; } = \"Pending\";",
    "    [Column(\"passenger_name\")] public string? PassengerName { get; set; }",
    "    [Column(\"age\")]         public int?    Age           { get; set; }",
    "    [Column(\"gender\")]      public string? Gender        { get; set; }",
    "    [Column(\"booking_group_id\")] public int? BookingGroupId { get; set; }",
    "}",
])
doc.add_paragraph()

subheading("3.3 How it helps")
bullets = [
    ("Where:", "Backend/Models/ — Booking.cs, Feedback.cs, Bus.cs, User.cs, BookingGroup.cs"),
    ("What:",  "Each class groups related properties using { get; set; } — no public fields, no scattered variables"),
    ("Why:",   "Data integrity is protected; you cannot accidentally write to an internal field from outside the class"),
    ("Benefit:", "Objects are self-contained; a Booking object carries everything needed to describe one seat reservation"),
]
for label, text in bullets:
    p = doc.add_paragraph()
    p.paragraph_format.left_indent = Inches(0.3)
    p.paragraph_format.space_after = Pt(3)
    r1 = p.add_run(label + " ")
    r1.bold = True
    r1.font.color.rgb = TEAL
    r1.font.size = Pt(11)
    r2 = p.add_run(text)
    r2.font.size = Pt(11)
    r2.font.color.rgb = DARK

subheading("3.4 What I understand better now")
body(
    "Before today I thought encapsulation was just 'making things private'. Now I understand it is "
    "about designing each class to own its data — the Booking class is the single source of truth "
    "for a seat booking. Nothing outside the class decides what a booking looks like."
)

doc.add_page_break()

# ═══════════════════════════════════════════════════════════════════════════════
#  SECTION 4 — ABSTRACTION
# ═══════════════════════════════════════════════════════════════════════════════
heading("4. Abstraction", 1, RED)
divider()

info_box("Status:", "Already present — controllers abstract away DB and SMTP details from the Angular frontend")

subheading("4.1 What is Abstraction?")
body(
    "Abstraction means exposing only what is necessary and hiding internal implementation details. "
    "A caller does not need to know HOW something works — only WHAT it can do."
)

subheading("4.2 Where is it used?  —  Backend/Controllers/ and Backend/Interfaces/IEmailService.cs")

h3("4.2a  Controllers hide database logic")
body("The Angular frontend calls a clean REST endpoint. It has no idea that Entity Framework or PostgreSQL are involved.")
code_block([
    "// Frontend sees only: POST /api/booking/lock",
    "// Backend hides everything:",
    "[HttpPost(\"lock\")]",
    "public IActionResult LockSeat([FromBody] Booking booking)",
    "{",
    "    var exists = context.Bookings.Any(b =>",
    "        b.BusId == booking.BusId && b.SeatNumber == booking.SeatNumber &&",
    "        (b.Status == \"Booked\" || b.Status == \"Paid\" ||",
    "        (b.Status == \"Locked\" && b.LockExpiry > DateTime.UtcNow)));",
    "",
    "    if (exists) return BadRequest(\"Seat already taken or locked\");",
    "    // ... lock and save",
    "}",
])
doc.add_paragraph()

h3("4.2b  IEmailService — interface abstraction  (NEW)")
body(
    "After today's session, the IEmailService interface was introduced. "
    "Controllers now depend only on the interface — they never see the SMTP/MailKit code."
)
code_block([
    "// File: Backend/Interfaces/IEmailService.cs",
    "namespace Backend.Interfaces",
    "{",
    "    public interface IEmailService",
    "    {",
    "        void SendBookingEmail(string to, string subject, string htmlBody);",
    "        void SendBookingEmail(string to, string subject);   // overload",
    "    }",
    "}",
])
doc.add_paragraph()

subheading("4.3 How it helps")
for label, text in [
    ("Where:",   "BookingController, FeedbackController inject IEmailService — not EmailService"),
    ("What:",    "The interface hides Gmail SMTP, MailKit, app-password config, and HTML building"),
    ("Why:",     "If we switch from Gmail to SendGrid tomorrow, we create a new class implementing IEmailService — zero controller changes"),
    ("Benefit:", "Clean separation between 'what the system can do' and 'how it does it'"),
]:
    p = doc.add_paragraph()
    p.paragraph_format.left_indent = Inches(0.3)
    p.paragraph_format.space_after = Pt(3)
    r1 = p.add_run(label + " "); r1.bold = True; r1.font.color.rgb = TEAL; r1.font.size = Pt(11)
    r2 = p.add_run(text);        r2.font.size = Pt(11); r2.font.color.rgb = DARK

doc.add_page_break()

# ═══════════════════════════════════════════════════════════════════════════════
#  SECTION 5 — INHERITANCE
# ═══════════════════════════════════════════════════════════════════════════════
heading("5. Inheritance", 1, RED)
divider()

info_box("Status:", "Already present — all controllers inherit from ASP.NET's ControllerBase")

subheading("5.1 What is Inheritance?")
body(
    "Inheritance lets a child class acquire the properties and methods of a parent class, "
    "enabling code reuse and framework integration without rewriting boilerplate."
)

subheading("5.2 Where is it used?  —  Every Controller file")
code_block([
    "// File: Backend/Controllers/BookingController.cs",
    "public class BookingController : ControllerBase   // inherits HTTP features",
    "{",
    "    // Inherited from ControllerBase:",
    "    //   Ok(), BadRequest(), NotFound(), Conflict(), Unauthorized(), ...",
    "    //   ModelState, HttpContext, RouteData, Request, Response",
    "}",
    "",
    "// Same pattern in every controller:",
    "public class AdminController    : ControllerBase { ... }",
    "public class FeedbackController : ControllerBase { ... }",
    "public class BusController      : ControllerBase { ... }",
    "public class OperatorController : ControllerBase { ... }",
    "public class UserController     : ControllerBase { ... }",
])
doc.add_paragraph()

subheading("5.3 How it helps")
for label, text in [
    ("Where:",   "Backend/Controllers/ — all 6 controller classes"),
    ("What:",    "Each controller inherits Ok(), BadRequest(), NotFound() and the full HTTP pipeline from ControllerBase"),
    ("Why:",     "We write zero HTTP-handling code — the framework's parent class provides it all"),
    ("Benefit:", "Thousands of lines of battle-tested HTTP code reused for free; we focus only on business logic"),
]:
    p = doc.add_paragraph()
    p.paragraph_format.left_indent = Inches(0.3)
    p.paragraph_format.space_after = Pt(3)
    r1 = p.add_run(label + " "); r1.bold = True; r1.font.color.rgb = TEAL; r1.font.size = Pt(11)
    r2 = p.add_run(text);        r2.font.size = Pt(11); r2.font.color.rgb = DARK

subheading("5.4 What I understand better now")
body(
    "Inheritance is not just for our own custom parent classes. ASP.NET's ControllerBase is a "
    "powerful parent built by Microsoft. Every time I write ': ControllerBase' I am inheriting "
    "an entire HTTP framework — that is real-world inheritance at scale."
)

doc.add_page_break()

# ═══════════════════════════════════════════════════════════════════════════════
#  SECTION 6 — POLYMORPHISM
# ═══════════════════════════════════════════════════════════════════════════════
heading("6. Polymorphism", 1, RED)
divider()

info_box("Status:", "Method overriding was implicit via framework. Method overloading was ADDED as a new enhancement today.")

subheading("6.1 What is Polymorphism?")
body(
    "Polymorphism means 'many forms'. The same method name behaves differently depending on "
    "context — either via overloading (different parameter signatures) or overriding "
    "(redefining a parent class's method in a child class)."
)

subheading("6.2 Method Overriding  —  ASP.NET Lifecycle  (Already Present)")
body(
    "ASP.NET's ControllerBase exposes virtual lifecycle hooks. Any controller can override these "
    "to add cross-cutting behaviour (logging, auth checks, etc.)."
)
code_block([
    "// Example of overriding an ASP.NET lifecycle method:",
    "public override void OnActionExecuting(ActionExecutingContext context)",
    "{",
    "    // Custom logic runs before every action in this controller",
    "    base.OnActionExecuting(context);",
    "}",
    "",
    "// The framework calls the same method name on every controller,",
    "// but each controller can give it a different implementation — polymorphism.",
])
doc.add_paragraph()

subheading("6.3 Method Overloading  —  EmailService  (NEW Enhancement)")
body(
    "Today, a second overload of SendBookingEmail was added to both IEmailService and EmailService. "
    "The same method name now handles two different calling situations."
)
code_block([
    "// File: Backend/Interfaces/IEmailService.cs",
    "public interface IEmailService",
    "{",
    "    // Overload 1 — caller provides full HTML body",
    "    void SendBookingEmail(string to, string subject, string htmlBody);",
    "",
    "    // Overload 2 — convenience; body is auto-generated from the subject",
    "    void SendBookingEmail(string to, string subject);",
    "}",
    "",
    "// File: Backend/Services/EmailService.cs",
    "public void SendBookingEmail(string toEmail, string subject, string htmlBody)",
    "{",
    "    // ... builds MimeMessage and sends via Gmail SMTP",
    "}",
    "",
    "public void SendBookingEmail(string toEmail, string subject)",
    "{",
    "    var defaultBody = $\"<div>...{subject}...</div>\";",
    "    SendBookingEmail(toEmail, subject, defaultBody);  // delegates to overload 1",
    "}",
])
doc.add_paragraph()

subheading("6.4 How it helps")
for label, text in [
    ("Where:",   "Backend/Interfaces/IEmailService.cs  and  Backend/Services/EmailService.cs"),
    ("What:",    "Two versions of SendBookingEmail — full control (3 params) and quick notification (2 params)"),
    ("Why:",     "Callers choose the right version for their context without needing different method names"),
    ("Benefit:", "Clean, readable API — the email service is flexible without being complicated"),
]:
    p = doc.add_paragraph()
    p.paragraph_format.left_indent = Inches(0.3)
    p.paragraph_format.space_after = Pt(3)
    r1 = p.add_run(label + " "); r1.bold = True; r1.font.color.rgb = TEAL; r1.font.size = Pt(11)
    r2 = p.add_run(text);        r2.font.size = Pt(11); r2.font.color.rgb = DARK

subheading("6.5 What I understand better now")
body(
    "I now understand that overloading is not just convenience — it is polymorphism. The compiler "
    "picks the right method at compile time based on how many arguments you pass. "
    "Overriding happens at runtime — the correct child-class version is chosen dynamically. "
    "Both are polymorphism, but they work at different stages."
)

doc.add_page_break()

# ═══════════════════════════════════════════════════════════════════════════════
#  SECTION 7 — INTERFACES
# ═══════════════════════════════════════════════════════════════════════════════
heading("7. Interfaces", 1, RED)
divider()

info_box("Status:", "NEW — IEmailService interface created today; controllers updated to depend on the interface, not the concrete class")

subheading("7.1 What is an Interface?")
body(
    "An interface is a contract. It declares method signatures (what must exist) without providing "
    "any implementation (how it works). Any class that 'implements' the interface must provide "
    "concrete implementations for every method declared."
)

subheading("7.2 Files involved")
rows_data = [
    ("Backend/Interfaces/IEmailService.cs",  "NEW",      "Declares the contract — two SendBookingEmail signatures"),
    ("Backend/Services/EmailService.cs",     "UPDATED",  "Implements IEmailService; contains real Gmail SMTP code"),
    ("Backend/Program.cs",                   "UPDATED",  "Registers: AddScoped<IEmailService, EmailService>()"),
    ("Backend/Controllers/BookingController.cs",  "UPDATED",  "Injects IEmailService — not EmailService directly"),
    ("Backend/Controllers/FeedbackController.cs", "UPDATED",  "Injects IEmailService — not EmailService directly"),
]
tbl = doc.add_table(rows=1, cols=3)
tbl.style = "Table Grid"
for i, txt in enumerate(["File", "Status", "Role"]):
    c = tbl.rows[0].cells[i]
    c.text = txt
    shade_cell(c, "1A2E55")
    for p in c.paragraphs:
        for r in p.runs:
            r.bold = True
            r.font.color.rgb = RGBColor(0xFF, 0xFF, 0xFF)
            r.font.size = Pt(10)

fills = {"NEW": "D5F5E3", "UPDATED": "D6EAF8"}
for file, status, role in rows_data:
    row = tbl.add_row().cells
    row[0].text = file
    row[1].text = status
    row[2].text = role
    shade_cell(row[1], fills.get(status, "FFFFFF"))
    for cell in row:
        for p in cell.paragraphs:
            for r in p.runs:
                r.font.size = Pt(9.5)
doc.add_paragraph()

subheading("7.3 The Interface — full code")
code_block([
    "// File: Backend/Interfaces/IEmailService.cs",
    "namespace Backend.Interfaces",
    "{",
    "    public interface IEmailService",
    "    {",
    "        // Full overload — caller provides HTML body",
    "        void SendBookingEmail(string to, string subject, string htmlBody);",
    "",
    "        // Convenience overload — body auto-generated",
    "        void SendBookingEmail(string to, string subject);",
    "    }",
    "}",
])
doc.add_paragraph()

subheading("7.4 EmailService implements the interface")
code_block([
    "// File: Backend/Services/EmailService.cs",
    "using Backend.Interfaces;",
    "",
    "public class EmailService : IEmailService   // ← implements the contract",
    "{",
    "    public void SendBookingEmail(string toEmail, string subject, string htmlBody)",
    "    {",
    "        // Real Gmail SMTP code using MailKit",
    "        var message = new MimeMessage();",
    "        message.From.Add(MailboxAddress.Parse(_fromEmail));",
    "        message.To.Add(MailboxAddress.Parse(toEmail));",
    "        message.Subject = subject;",
    "        message.Body    = new TextPart(\"html\") { Text = htmlBody };",
    "        using var smtp = new SmtpClient();",
    "        smtp.Connect(\"smtp.gmail.com\", 587, false);",
    "        smtp.Authenticate(_fromEmail, _appPassword);",
    "        smtp.Send(message);",
    "        smtp.Disconnect(true);",
    "    }",
    "",
    "    public void SendBookingEmail(string toEmail, string subject)",
    "    {",
    "        var defaultBody = $\"<div>...{subject}...</div>\";",
    "        SendBookingEmail(toEmail, subject, defaultBody);",
    "    }",
    "}",
])
doc.add_paragraph()

subheading("7.5 DI registration in Program.cs")
code_block([
    "// File: Backend/Program.cs",
    "// BEFORE (tightly coupled to concrete class):",
    "builder.Services.AddScoped<EmailService>();",
    "",
    "// AFTER (loosely coupled via interface):",
    "builder.Services.AddScoped<IEmailService, EmailService>();",
    "//  ↑ any IEmailService injected will receive an EmailService instance",
])
doc.add_paragraph()

subheading("7.6 Controllers use the interface, not the class")
code_block([
    "// File: Backend/Controllers/BookingController.cs",
    "// BEFORE:",
    "public class BookingController(AppDbContext context, EmailService emailService) : ControllerBase",
    "",
    "// AFTER — depends on interface, not implementation:",
    "public class BookingController(AppDbContext context, IEmailService emailService) : ControllerBase",
    "//                                                    ^^ interface only",
    "",
    "// Same change applied to FeedbackController",
])
doc.add_paragraph()

subheading("7.7 How it helps")
for label, text in [
    ("Where:",    "Interfaces/ folder → IEmailService; Services/ folder → EmailService"),
    ("What:",     "The interface separates the contract from the implementation"),
    ("Why:",      "BookingController should not care whether email is sent via Gmail or SendGrid or a mock"),
    ("Benefit 1:", "Loose coupling — swap implementation without changing any controller"),
    ("Benefit 2:", "Easy testing — inject a mock IEmailService in unit tests, no real email sent"),
    ("Benefit 3:", "Open/Closed Principle — add new email providers by adding new classes, not editing existing ones"),
]:
    p = doc.add_paragraph()
    p.paragraph_format.left_indent = Inches(0.3)
    p.paragraph_format.space_after = Pt(3)
    r1 = p.add_run(label + " "); r1.bold = True; r1.font.color.rgb = TEAL; r1.font.size = Pt(11)
    r2 = p.add_run(text);        r2.font.size = Pt(11); r2.font.color.rgb = DARK

subheading("7.8 What I understand better now")
body(
    "An interface is a promise. When BookingController asks for an IEmailService, it is saying: "
    "'give me anything that knows how to send a booking email — I do not care what is inside.' "
    "This makes the controller completely independent of how email is delivered. "
    "If the company changes email providers, no controller code changes — only the injected class changes."
)

doc.add_page_break()

# ═══════════════════════════════════════════════════════════════════════════════
#  SECTION 8 — CODING STANDARDS
# ═══════════════════════════════════════════════════════════════════════════════
heading("8. Coding Standards", 1, RED)
divider()

info_box("Status:", "Partially present — short methods and clean structure already existed. Switch statement added today.")

subheading("8.1 Short, Single-Purpose Methods  (Already Present)")
body(
    "Every controller action does exactly one thing. The method name tells you what it does, "
    "and the body delivers that one thing — no mixing of concerns."
)
code_block([
    "// File: Backend/Controllers/BookingController.cs",
    "// Each method = one responsibility:",
    "",
    "LockSeat()          — locks one seat for 5 minutes",
    "CreateBooking()     — confirms a locked seat as booked",
    "FinalizeGroup()     — groups individual bookings into one payment unit",
    "PayGroup()          — marks all seats paid and sends confirmation email",
    "GetGroupTicket()    — fetches full ticket details for a group",
    "CancelBooking()     — cancels a booking and initiates refund if paid",
    "GetBookedSeats()    — returns seat map data (booked/locked/gender)",
])
doc.add_paragraph()

subheading("8.2 Switch Statements  (NEW Enhancement)")
body(
    "The UserController.Register method previously used an if statement to handle role-based "
    "logic. It has been refactored to a switch statement, making it ready for future roles "
    "and easier to read."
)

code_block([
    "// File: Backend/Controllers/UserController.cs",
    "",
    "// BEFORE — brittle if statement:",
    "if (dto.Role == \"Operator\")",
    "{",
    "    _context.OperatorProfiles.Add(new OperatorProfile { ... });",
    "    _context.SaveChanges();",
    "}",
    "",
    "// AFTER — clean switch statement:",
    "switch (user.Role)",
    "{",
    "    case \"Operator\":",
    "        var profile = new OperatorProfile",
    "        {",
    "            UserId       = user.Id,",
    "            BusinessName = user.Name,",
    "            IsApproved   = false,",
    "            AppliedAt    = DateTime.UtcNow",
    "        };",
    "        _context.OperatorProfiles.Add(profile);",
    "        _context.SaveChanges();",
    "        break;",
    "",
    "    case \"Admin\":",
    "    case \"Customer\":",
    "        // no extra setup required",
    "        break;",
    "}",
])
doc.add_paragraph()

body(
    "Why switch is better:",
    bold_parts=["Why switch is better:"]
)
bullet("All role-handling logic is in one place — no scattered if/else if chains")
bullet("Adding a new role (e.g. 'Moderator') means adding one new case block — nothing else changes")
bullet("The default case can catch unexpected roles with an error, making bugs visible early")

subheading("8.3 Clean Folder Structure  (Already Present, Reinforced Today)")
body("The backend follows a layered architecture — each folder owns one layer of responsibility:")
for folder, rule in [
    ("Models/",      "Data shape only — no logic, no DB calls"),
    ("Controllers/", "HTTP routing only — validates input, calls services/db, returns response"),
    ("Services/",    "Business logic only — email, calculations, notifications"),
    ("Interfaces/",  "Contracts only — no implementation, no dependencies"),
    ("Data/",        "DB session only — DbContext and DbSets"),
]:
    p = doc.add_paragraph()
    p.paragraph_format.left_indent = Inches(0.3)
    p.paragraph_format.space_after = Pt(3)
    r1 = p.add_run(folder + "  "); r1.bold = True; r1.font.color.rgb = TEAL; r1.font.size = Pt(11)
    r2 = p.add_run(rule);          r2.font.size = Pt(11); r2.font.color.rgb = DARK

doc.add_paragraph()
subheading("8.4 What I understand better now")
body(
    "Coding standards are not about following rules for their own sake — they reduce cognitive load. "
    "When every method does one thing, I can read the code top to bottom and understand the entire "
    "booking workflow in minutes. The switch statement is a signal to every future developer: "
    "'all role logic lives here, add your case here.' That is real team communication through code."
)

doc.add_page_break()

# ═══════════════════════════════════════════════════════════════════════════════
#  SECTION 9 — SERVICE LAYER DESIGN
# ═══════════════════════════════════════════════════════════════════════════════
heading("9. Service Layer Design", 1, RED)
divider()

info_box("Status:", "Already present — EmailService separates email logic from controllers. Enhanced today by adding IEmailService.")

subheading("9.1 What is the Service Layer?")
body(
    "The Service Layer is a design pattern that places business logic in dedicated service classes, "
    "separate from controllers. Controllers handle HTTP concerns only; services handle the actual work."
)

subheading("9.2 EmailService  —  Backend/Services/EmailService.cs")
body("The email service encapsulates ALL email-related responsibilities:")
bullet("Reads SMTP credentials from appsettings.json configuration")
bullet("Builds a MimeMessage with From, To, Subject, and HTML body")
bullet("Connects to Gmail SMTP server (smtp.gmail.com:587)")
bullet("Authenticates, sends, and disconnects cleanly")
bullet("Skips gracefully if credentials are not configured (dev environment safety)")
bullet("Provides two SendBookingEmail overloads for flexible usage")

subheading("9.3 How it helps")
for label, text in [
    ("Where:",    "Backend/Services/EmailService.cs implements Backend/Interfaces/IEmailService"),
    ("What:",     "All email logic — credentials, SMTP, HTML building — is in one class"),
    ("Why:",      "Controllers (BookingController, FeedbackController) stay clean and short"),
    ("Benefit:",  "If email breaks, we look in one file. If we add SMS, we add one new service — nothing else changes"),
]:
    p = doc.add_paragraph()
    p.paragraph_format.left_indent = Inches(0.3)
    p.paragraph_format.space_after = Pt(3)
    r1 = p.add_run(label + " "); r1.bold = True; r1.font.color.rgb = TEAL; r1.font.size = Pt(11)
    r2 = p.add_run(text);        r2.font.size = Pt(11); r2.font.color.rgb = DARK

doc.add_page_break()

# ═══════════════════════════════════════════════════════════════════════════════
#  SECTION 10 — BEFORE vs AFTER
# ═══════════════════════════════════════════════════════════════════════════════
heading("10. Before vs After Applying OOP", 1, RED)
divider()

tbl2 = doc.add_table(rows=1, cols=3)
tbl2.style = "Table Grid"
for i, txt in enumerate(["Aspect", "Before", "After"]):
    c = tbl2.rows[0].cells[i]
    c.text = txt
    shade_cell(c, "1A2E55")
    for p in c.paragraphs:
        for r in p.runs:
            r.bold = True
            r.font.color.rgb = RGBColor(0xFF, 0xFF, 0xFF)
            r.font.size = Pt(10.5)

comparisons = [
    ("Email service injection", "Injected EmailService (concrete class)", "Injects IEmailService (interface)"),
    ("Email provider change", "Edit BookingController + FeedbackController", "Create new class, update Program.cs only"),
    ("Role handling in Register", "if (role == \"Operator\") {...}", "switch(user.Role) — all roles in one block"),
    ("Unit testing email", "Hard — real SMTP called in tests", "Easy — inject a mock IEmailService"),
    ("Email method variants", "One method — must always pass HTML body", "Two overloads — quick notification possible"),
    ("Interfaces folder", "Did not exist", "Backend/Interfaces/IEmailService.cs created"),
    ("DI registration", "AddScoped<EmailService>()", "AddScoped<IEmailService, EmailService>()"),
]
for aspect, before, after in comparisons:
    row = tbl2.add_row().cells
    row[0].text = aspect
    row[1].text = before
    row[2].text = after
    shade_cell(row[1], "FDECEA")
    shade_cell(row[2], "E8F8F5")
    for cell in row:
        for p in cell.paragraphs:
            for r in p.runs:
                r.font.size = Pt(10)

doc.add_page_break()

# ═══════════════════════════════════════════════════════════════════════════════
#  SECTION 11 — SUMMARY TABLE
# ═══════════════════════════════════════════════════════════════════════════════
heading("11. Summary — All OOP Concepts at a Glance", 1, RED)
divider()

tbl3 = doc.add_table(rows=1, cols=4)
tbl3.style = "Table Grid"
for i, txt in enumerate(["OOP Concept", "File / Location", "Status", "Key Benefit"]):
    c = tbl3.rows[0].cells[i]
    c.text = txt
    shade_cell(c, "C02525")
    for p in c.paragraphs:
        for r in p.runs:
            r.bold = True
            r.font.color.rgb = RGBColor(0xFF, 0xFF, 0xFF)
            r.font.size = Pt(10)

summary_rows = [
    ("Encapsulation",        "Backend/Models/Booking.cs\nand all other model files",           "Already Present", "Self-contained data objects; data integrity protected"),
    ("Abstraction",          "Backend/Controllers/\nBackend/Interfaces/IEmailService.cs",       "Present + Enhanced","Frontend/controllers hide DB and SMTP details completely"),
    ("Inheritance",          "All 6 Controllers\n(: ControllerBase)",                           "Already Present", "HTTP handling reused from ASP.NET framework"),
    ("Method Overriding",    "ControllerBase lifecycle hooks\n(OnActionExecuting)",              "Already Present", "Custom cross-cutting logic per controller"),
    ("Method Overloading",   "Backend/Interfaces/IEmailService.cs\nBackend/Services/EmailService.cs","NEW Added",  "Two SendBookingEmail signatures for different use cases"),
    ("Interfaces",           "Backend/Interfaces/IEmailService.cs",                             "NEW Added",       "Loose coupling; swap email provider without controller changes"),
    ("Switch Statement",     "Backend/Controllers/UserController.cs",                           "NEW Added",       "All role logic in one readable, extendable block"),
    ("Service Layer",        "Backend/Services/EmailService.cs",                                "Already Present", "Email logic isolated; controllers stay clean"),
    ("Clean Structure",      "Models/ Controllers/ Services/\nInterfaces/ Data/ folders",       "Already Present", "One responsibility per folder; easy navigation"),
]
status_colors = {
    "Already Present": "D5F5E3",
    "Present + Enhanced": "D6EAF8",
    "NEW Added": "FEF9E7",
}
for concept, location, status, benefit in summary_rows:
    row = tbl3.add_row().cells
    row[0].text = concept
    row[1].text = location
    row[2].text = status
    row[3].text = benefit
    shade_cell(row[2], status_colors.get(status, "FFFFFF"))
    for cell in row:
        for p in cell.paragraphs:
            for r in p.runs:
                r.font.size = Pt(9.5)
doc.add_paragraph()

doc.add_page_break()

# ═══════════════════════════════════════════════════════════════════════════════
#  SECTION 12 — CONCLUSION
# ═══════════════════════════════════════════════════════════════════════════════
heading("12. Conclusion", 1, RED)
divider()

body(
    "The Bus Booking System already applied several OOP principles correctly before today's session. "
    "Today's enhancements completed the picture:"
)
bullet("IEmailService interface introduced → true loose coupling between controllers and email delivery")
bullet("Method overloading added → the email API is now flexible without being cluttered")
bullet("switch(user.Role) replaces if → role logic is centralised, readable, and extensible")
bullet("Interfaces/ folder created → the project now follows the complete layered architecture pattern")

body(
    "\nThe core insight from today is that OOP principles are not isolated rules — they work together. "
    "Encapsulation makes models trustworthy. Abstraction (via interfaces) makes services swappable. "
    "Inheritance gives controllers their HTTP powers. Polymorphism (overloading + overriding) makes "
    "the API flexible. Coding standards make the whole system understandable at a glance.",
    bold_parts=["Encapsulation", "Abstraction", "Inheritance", "Polymorphism", "Coding standards"]
)

body(
    "\nThe result is a codebase where each piece has one job, each dependency is behind an interface, "
    "and every future developer — or future version of ourselves — can navigate, extend, "
    "and maintain the system with confidence."
)

# Footer author line
doc.add_paragraph()
divider()
foot = doc.add_paragraph()
foot.alignment = WD_ALIGN_PARAGRAPH.CENTER
rf  = foot.add_run("OOPS in Project  ·  Bus Booking System  ·  Angular + .NET + PostgreSQL")
rf.font.size = Pt(9)
rf.font.color.rgb = GREY
rf.italic = True

# ── Save ──────────────────────────────────────────────────────────────────────
out = r"c:\Users\ragha\OneDrive\Desktop\Nikitha\genspark-training\BusBooking\OOPS_in_Project_BusBooking.docx"
doc.save(out)
print("Saved:", out)
