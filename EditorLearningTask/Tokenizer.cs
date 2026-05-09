using System.Threading.Channels;

namespace EditorLearningTask;

public sealed class Tokenizer
{
    private const int BatchSize = 30;

    // Pre-allocated buffers reused across every FlushTokens call
    private readonly string[] _textBuffer = new string[BatchSize];
    private readonly IReadOnlyList<Token>[] _tokenOutput = new IReadOnlyList<Token>[BatchSize];
    // Survives across batch calls so multi-line tokens (comments, strings) span batch boundaries
    private int _lineEndState;

    public async Task ProduceTokens(
        ChannelReader<LineItem> lineItemsReader,
        ChannelWriter<CodeLine> tokensWriter,
        CancellationToken ct)
    {
        try
        {
            var lineItemsBatch = new List<LineItem>(BatchSize);
            await foreach (var lineItem in lineItemsReader.ReadAllAsync(ct))
            {
                lineItemsBatch.Add(lineItem);
                if (lineItemsBatch.Count >= BatchSize)
                {
                    await FlushTokens(lineItemsBatch, tokensWriter, ct);
                    lineItemsBatch.Clear();
                }
            }

            if (lineItemsBatch.Count > 0)
                await FlushTokens(lineItemsBatch, tokensWriter, ct);
        }
        catch (OperationCanceledException) { /* shutdown */ }
        catch (Exception exception)
        {
            tokensWriter.Complete(exception);
        }
        finally
        {
            tokensWriter.TryComplete();
        }
    }

    private async Task FlushTokens(
        List<LineItem> lineItems,
        ChannelWriter<CodeLine> output,
        CancellationToken ct)
    {
        for (int i = 0; i < lineItems.Count; i++)
        {
            _textBuffer[i] = lineItems[i].Text;
        }

        Lexer.Tokenize(_textBuffer, lineItems.Count, _tokenOutput, ref _lineEndState);

        for (int i = 0; i < lineItems.Count; i++)
        {
            await output.WriteAsync(new CodeLine(lineItems[i].Index, _textBuffer[i], _tokenOutput[i]), ct);
        }
    }
}
