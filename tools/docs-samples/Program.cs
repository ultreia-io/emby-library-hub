// Render fictional documentation reports with the production HTML renderer.
// No Emby server, saved configuration, subscriber store, or SMTP service is used.
using System;
using System.IO;
using Emby.LibraryHub.Core;

if (args.Length != 1) throw new ArgumentException("Pass the output directory for demonstration HTML.");
Directory.CreateDirectory(args[0]);
foreach (var language in new[] { "en", "fr" })
{
    var fr = language == "fr";
    var title = fr ? "Les nouveautés du 11 octobre 2026" : "Library updates · 11 October 2026";
    var body = title + "\n\n" + (fr ? "Cinéma\nAjouts :" : "Cinema\nAdded:") +
        "\n• Arrival (2016)\n  https://media.example.test/web/index.html#!/item?id=101" +
        "\n• The Grand Budapest Hotel (2014)\n  https://media.example.test/web/index.html#!/item?id=102" +
        (fr ? "\nRetraits :\n• Un ancien titre\n\nSéries\nAjouts :" : "\nRemoved:\n• A retired title\n\nSeries\nAdded:") +
        "\n• Silo — " + (fr ? "Saison 1 · épisodes 1 à 3" : "Season 1 · episodes 1–3") +
        "\n  https://media.example.test/web/index.html#!/item?id=200";
    var message = new DigestMessage { Subject = title, Body = body };
    File.WriteAllText(Path.Combine(args[0], $"archive.{language}.html"),
        ReportHtml.Day(message, language, false, "/emby/LibraryHub/Archive"));
}
Console.WriteLine("Rendered English and French demo reports with ReportHtml.Day.");
