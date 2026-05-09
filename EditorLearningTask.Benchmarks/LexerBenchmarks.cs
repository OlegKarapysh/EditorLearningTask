using BenchmarkDotNet.Attributes;
using EditorLearningTask;

namespace EditorLearningTask.Benchmarks;

/// <summary>
/// Benchmarks for <see cref="Lexer.Tokenize"/>.
///
/// Run with:  dotnet run -c Release --project EditorLearningTask.Benchmarks
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "RatioSD", "Ratio")]
public class LexerBenchmarks
{
    // 30 lines — matches Tokenizer.BatchSize; covers every token type:
    // keywords, identifiers, strings (with '' escapes), numbers, symbols,
    // single-line comments, and a multi-line block comment.
    private static readonly string[] BatchLines =
    [
        "SELECT u.id, u.name, COUNT(o.id) AS order_count",
        "FROM users u",
        "LEFT JOIN orders o ON u.id = o.user_id",
        "WHERE u.id > 0 AND u.name IS NOT NULL",
        "GROUP BY u.id, u.name",
        "HAVING COUNT(o.id) > 0",
        "ORDER BY order_count DESC",
        "LIMIT 100;",
        "-- Retrieve active users only",
        "SELECT id, COALESCE(name, 'Unknown') AS display_name FROM users WHERE active = TRUE;",
        "INSERT INTO users (id, name, email) VALUES (1, 'Alice', 'alice@example.com');",
        "INSERT INTO users (id, name, email) VALUES (2, 'Bob', 'it''s bob@example.com');",
        "UPDATE users SET email = 'alice@newdomain.com', active = TRUE WHERE id = 1;",
        "DELETE FROM users WHERE id = 2;",
        "CREATE TABLE orders (id INT PRIMARY KEY, user_id INT, amount VARCHAR(50));",
        "/*",
        "  Multi-line block comment.",
        "  Contains SQL keywords: SELECT FROM WHERE — should all be TOKEN_COMMENT.",
        "*/",
        "BEGIN TRANSACTION;",
        "UPDATE orders SET amount = 99 WHERE user_id = 1;",
        "COMMIT;",
        "CREATE FUNCTION get_user_count() RETURNS INT AS $$ BEGIN RETURN (SELECT COUNT(*) FROM users); END; $$;",
        "SELECT CASE WHEN status = 'active' THEN 1 ELSE 0 END AS is_active FROM users;",
        "SELECT DISTINCT user_id FROM orders WHERE amount > 50 AND amount < 200;",
        "SELECT NULL, TRUE, FALSE, 42, 3, 'string with ''escaped'' quotes';",
        "SELECT * FROM users INNER JOIN orders ON users.id = orders.user_id;",
        "DELETE FROM logs WHERE id < 100;",
        "-- End of batch",
        "SELECT COUNT(DISTINCT user_id) FROM orders GROUP BY user_id HAVING COUNT(*) > 1;",
    ];

    // A single long line that exercises every branch (keywords, identifiers,
    // strings, numbers, symbols) in one pass — useful for per-line profiling.
    private static readonly string[] SingleLine =
    [
        "SELECT u.id, u.name, COUNT(o.id) AS cnt FROM users u LEFT JOIN orders o ON u.id = o.user_id WHERE u.active = TRUE AND o.amount > 0 GROUP BY u.id ORDER BY cnt DESC LIMIT 50;",
    ];

    private readonly IReadOnlyList<Token>[] _batchOutput = new IReadOnlyList<Token>[BatchLines.Length];
    private readonly IReadOnlyList<Token>[] _singleOutput = new IReadOnlyList<Token>[1];

    [Benchmark(Baseline = true, Description = "30-line SQL batch")]
    public void TokenizeBatch()
    {
        int lineEndState = 0;
        Lexer.Tokenize(BatchLines, BatchLines.Length, _batchOutput, ref lineEndState);
    }

    [Benchmark(Description = "Single dense SQL line")]
    public void TokenizeSingleLine()
    {
        int lineEndState = 0;
        Lexer.Tokenize(SingleLine, 1, _singleOutput, ref lineEndState);
    }
}
