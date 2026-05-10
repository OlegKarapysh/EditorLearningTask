namespace EditorLearningTask;

public readonly record struct Token(int Start, int Length, int Value, ReadOnlyMemory<char> Text);