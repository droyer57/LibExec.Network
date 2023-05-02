using System.Text;

namespace LibExec.Network.SourceGenerators.Builder;

internal class BuilderBase
{
    private const int IndentSpaces = 4;
    private readonly StringBuilder _stringBuilder;

    private string _indent = "";
    private bool _isFirstMember = true;
    private bool _wasLastCallAppendLine = true;

    protected BuilderBase()
    {
        _stringBuilder = new StringBuilder();
    }

    private int IndentLevel { get; set; }

    public void IncreaseIndent()
    {
        IndentLevel++;
        _indent += new string(' ', IndentSpaces);
    }

    public bool DecreaseIndent()
    {
        if (_indent.Length >= IndentSpaces)
        {
            IndentLevel--;
            _indent = _indent.Substring(IndentSpaces);
            return true;
        }

        return false;
    }

    public void AppendLineBeforeMember()
    {
        if (!_isFirstMember)
        {
            _stringBuilder.AppendLine();
        }

        _isFirstMember = false;
    }

    public void AppendLine(string line)
    {
        if (_wasLastCallAppendLine)
        {
            _stringBuilder.Append(_indent);
        }

        _stringBuilder.AppendLine($"{line}");
        _wasLastCallAppendLine = true;
    }

    public void AppendLine()
    {
        _stringBuilder.AppendLine();
        _wasLastCallAppendLine = true;
    }

    public void Append(string stringToAppend)
    {
        if (_wasLastCallAppendLine)
        {
            _stringBuilder.Append(_indent);
            _wasLastCallAppendLine = false;
        }

        _stringBuilder.Append(stringToAppend);
    }

    public override string ToString()
    {
        return _stringBuilder.ToString();
    }
}