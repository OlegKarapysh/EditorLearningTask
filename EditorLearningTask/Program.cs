using EditorLearningTask;

if (args is ["generate", _, ..] && int.TryParse(args[1], out int requestedLines) && requestedLines > 0)
{
    Generator.GenerateSqlFile(requestedLines);
}

var filePath = "output.sql";

if (File.Exists(filePath))
{
    const int linesPerPage = 30;
    var time = new TimeMeasurement();
    using (time.Measure("Total time"))
    {
        var editor = new Editor(new Lexer(), new Colorizer(), new Reader());
        using (time.Measure("Time to display first page"))
        {
            editor.Initialize(filePath);
            editor.Display(0, linesPerPage);
        }

        using (time.Measure("Time to display second page"))
            editor.Display(100_000, linesPerPage);
        
        using (time.Measure("Time to display second page"))
            editor.Display(1_000_000, linesPerPage);
        editor.Edit();
    }
}
else
{
    Console.WriteLine($"File not found: {filePath}");
}