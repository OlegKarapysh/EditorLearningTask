using BenchmarkDotNet.Attributes;

namespace EditorLearningTask.Benchmarks;

/// <summary>
/// Shared input data. Static so both benchmark classes point at the same arrays
/// without any per-invocation allocation.
/// </summary>
file static class BenchmarkInput
{
    // 30 lines — matches Tokenizer.BatchSize. Covers every token type:
    // keywords, identifiers, strings (with '' escapes), numbers, symbols,
    // single-line comments (--), and a multi-line block comment (/* ... */).
    // The block comment is fully closed within the batch so lineEndState
    // returns to 0 after each invocation.
    public static readonly string[] Batch =
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
    // strings, numbers, symbols) in a single pass.
    public static readonly string[] SingleLine =
    [
        "SELECT u.id, u.name, COUNT(o.id) AS cnt FROM users u LEFT JOIN orders o ON u.id = o.user_id WHERE u.active = TRUE AND o.amount > 0 GROUP BY u.id ORDER BY cnt DESC LIMIT 50;",
    ];
}

// ---------------------------------------------------------------------------
// 30-line batch
// ---------------------------------------------------------------------------

/// <summary>
/// Compares old vs new Lexer on a 30-line SQL batch (= Tokenizer.BatchSize).
/// LexerOld is the baseline so the Ratio column shows the speedup of the new Lexer.
/// </summary>
[MemoryDiagnoser]
public class LexerBatchBenchmarks
{
    private readonly LexerOld _lexerOld = new();
    private readonly IReadOnlyList<Token>[] _output = new IReadOnlyList<Token>[BenchmarkInput.Batch.Length];

    [Benchmark(Baseline = true, Description = "Old")]
    public List<List<Token>> Old() => _lexerOld.Tokenize(BenchmarkInput.Batch);

    [Benchmark(Description = "New")]
    public void New()
    {
        int lineEndState = 0;
        Lexer.Tokenize(BenchmarkInput.Batch, BenchmarkInput.Batch.Length, _output, ref lineEndState);
    }
}

// ---------------------------------------------------------------------------
// Single-line
// ---------------------------------------------------------------------------

/// <summary>
/// Compares old vs new Lexer on a single dense SQL line.
/// Useful for understanding per-line cost in isolation.
/// </summary>
[MemoryDiagnoser]
public class LexerSingleLineBenchmarks
{
    private readonly LexerOld _lexerOld = new();
    private readonly IReadOnlyList<Token>[] _output = new IReadOnlyList<Token>[1];

    [Benchmark(Baseline = true, Description = "Old")]
    public List<List<Token>> Old() => _lexerOld.Tokenize(BenchmarkInput.SingleLine);

    [Benchmark(Description = "New")]
    public void New()
    {
        int lineEndState = 0;
        Lexer.Tokenize(BenchmarkInput.SingleLine, 1, _output, ref lineEndState);
    }
}
