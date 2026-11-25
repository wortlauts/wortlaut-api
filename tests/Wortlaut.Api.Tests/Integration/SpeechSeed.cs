namespace Wortlaut.Api.Tests.Integration;

internal static class SpeechSeed
{
    public const string AnnaMusterId = "21-10-00100";

    public static IReadOnlyList<object> All { get; } =
    [
        Speech(
            id: AnnaMusterId, period: 21, session: 10, date: "2025-11-14", agendaItem: "Digitale Verwaltung",
            speaker: Speaker("100", "Dr. Anna Muster", "SPD"),
            text: "Die Digitalisierung der Verwaltungen ist ein zentrales Thema. Wir brauchen eine soziale Marktwirtschaft.",
            reactions: [Reaction("applause", "SPD", null), Reaction("heckle", "Max Frager [AfD]", "Das stimmt nicht!")]),
        Speech(
            id: "21-10-00200", period: 21, session: 10, date: "2025-11-14", agendaItem: "Digitale Verwaltung",
            speaker: Speaker("200", "Max Frager", "AfD"),
            text: "Die Verwaltung muss schlanker werden. Die Digitalisierung kostet Geld.",
            reactions: [Reaction("laughter", "SPD", null)]),
        Speech(
            id: "21-20-00100", period: 21, session: 20, date: "2026-03-05", agendaItem: "Bürgergeld",
            speaker: Speaker("300", "Karl Beispiel", "CDU/CSU"),
            text: "Wir sichern die soziale Marktwirtschaft. Das Bürgergeld muss reformiert werden.",
            reactions: [Reaction("applause", "CDU/CSU", null), Reaction("objection", "Die Linke", null)]),
        Speech(
            id: "21-20-00200", period: 21, session: 20, date: "2026-03-05", agendaItem: "Bürgergeld",
            speaker: Speaker("400", "Lena Linke", "Die Linke"),
            text: "Das Bürgergeld muss bleiben.",
            reactions: []),
        Speech(
            id: "21-20-00300", period: 21, session: 20, date: "2026-03-05", agendaItem: "Bürgergeld",
            speaker: Speaker("500", "Peter Minister", faction: null, role: "Bundesminister BMF"),
            text: "Der Haushalt steht auf soliden Füßen.",
            reactions: []),
        Speech(
            id: "20-5-00100", period: 20, session: 5, date: "2022-06-01", agendaItem: "Breitband",
            speaker: Speaker("100", "Dr. Anna Muster", "SPD"),
            text: "Digitalisierung braucht schnelles Breitband.",
            reactions: [Reaction("applause", "SPD", null)]),
    ];

    private static object Speech(
        string id, int period, int session, string date, string agendaItem, object speaker, string text, object[] reactions) => new
        {
            id,
            legislative_period = period,
            session,
            date,
            agenda_item = agendaItem,
            delivery = "plenary",
            speaker,
            text,
            reactions,
            word_count = text.Split(' ').Length,
            source_url = $"https://dserver.bundestag.de/btp/{period}/{period}{session:000}.pdf",
            rights = new
            {
                license = "Official work (section 5 UrhG)",
                attribution = "Deutscher Bundestag",
                allowed_actions = new[] { "read", "quote" },
            },
            content_hash = new string('a', 64),
        };

    private static object Speaker(string id, string name, string? faction, string? role = null) =>
        new { id, name, faction, role };

    private static object Reaction(string type, string source, string? text) => new { type, source, text };
}
