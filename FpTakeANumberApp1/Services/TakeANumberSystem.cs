using System.ComponentModel;
using System.Speech.Synthesis;

namespace FpTakeANumberApp1;

public class TakeANumberSystem
{
    SpeechService _ss;
    private readonly object _lock = new();

    public int MaxCardNumber { get; private set; }
    public int ResetToNumber { get; private set; }
    public string ServedCount => _callHistory.Count.ToString();
    private int _nowServing = 0;
    public string NowServing => _nowServing == 0 ? "" : _nowServing.ToString();

    public int TicketCount { get; private set; }

    int _nextNumber = 1;
    public int NextNumber => _nextNumber;

    public record TanNumber(int Id, int CardNumber, int TableId, string Name, string Phone);

    private Dictionary<int, List<TanNumber>> _tables = new();
    private List<TanNumber> _callHistory = new();

    public int WorkerCount { get; private set; } = 4;

    private List<TanNumber> GetTableList(int tableId)
    {
        if (!_tables.ContainsKey(tableId))
        {
            _tables[tableId] = new List<TanNumber>();
        }
        return _tables[tableId];
    }

    public string GetTableDisplay(int tableId)
    {
        var list = GetTableList(tableId);
        return list.Count > 0 ? list.First().CardNumber.ToString() : "";
    }

    public List<string> GetTableHistory(int tableId)
    {
        var list = GetTableList(tableId);
        return list.ToList().ConvertAll(x => x.CardNumber.ToString());
    }

    public string Table1 => GetTableDisplay(1);
    public string Table2 => GetTableDisplay(2);
    public string Table3 => GetTableDisplay(3);
    public string Table4 => GetTableDisplay(4);

    public List<string> Table1History => GetTableHistory(1);
    public List<string> Table2History => GetTableHistory(2);
    public List<string> Table3History => GetTableHistory(3);
    public List<string> Table4History => GetTableHistory(4);

    public BindingList<ChatMessage> TextMessages { get; } = new BindingList<ChatMessage>();

    public bool IsPaused { get; private set; }
    public bool IsMuted { get; private set; }
    public bool ShowQR { get; private set; }

    public string NowServingTable => _callHistory.Count > 0 ? _callHistory.First().TableId.ToString() : "";
    public string NowServingTableLabel => _callHistory.Count > 0 ? $"Table {_callHistory.First().TableId}" : "";

    public TakeANumberSystem(IConfiguration cfg)
    {
        _ss = new SpeechService();
        int cfgWorkers = cfg.GetValue<int>("WorkerCount");
        if (cfgWorkers > 0) WorkerCount = cfgWorkers;
    }

    internal void Restart()
    {
        lock (_lock)
        {
            _tables.Clear();
            _nowServing = 0;
            _callHistory.Clear();
            IsMuted = false;
            ResetToNumber = 0;
            MaxCardNumber = 0;
            _nextNumber = 1;
            WorkerCount = 4;
        }
        SomethingInterestingIsChanging();
    }

    internal void ClearChat()
    {
        lock (_lock)
        {
            TextMessages.Clear();
        }
    }

    internal void SetTable(int id, bool useNaturalVoice)
    {
        TanNumber x;
        lock (_lock)
        {
            _nowServing = _nextNumber;
            _nextNumber++;

            if (_nextNumber > MaxCardNumber && MaxCardNumber > 0)
                _nextNumber = 1;

            ResetToNumber = 0;

            var nextId = _callHistory.Count + 1;
            x = new TanNumber(nextId, _nowServing, id, "", "");

            var list = GetTableList(id);
            list.Insert(0, x);

            _callHistory.Insert(0, x);
        }

        ReadAloud(id, useNaturalVoice);
        SomethingInterestingIsChanging();
    }

    internal void Undo()
    {
        TanNumber? x = null;
        lock (_lock)
        {
            if (_callHistory.Count > 0)
            {
                x = _callHistory.FirstOrDefault();
                if (x is not null)
                {
                    var list = GetTableList(x.TableId);
                    list.Remove(x);
                    _callHistory.Remove(x);

                    _nowServing = _callHistory.Count > 0 ? _callHistory.First().CardNumber : 0;
                    _nextNumber = _nowServing + 1;

                    if (_nextNumber > MaxCardNumber && MaxCardNumber != 0)
                        _nextNumber = 1;
                }
            }
        }
        if (x is not null)
            SomethingInterestingIsChanging();
    }

    internal void SetMaxCard(int cardNumber)
    {
        lock (_lock)
        {
            MaxCardNumber = cardNumber;
        }
        SomethingInterestingIsChanging();
    }

    internal void ResetNowServing(int number)
    {
        lock (_lock)
        {
            _nextNumber = number;
            ResetToNumber = _nextNumber;
        }
        SomethingInterestingIsChanging();
    }

    internal void SetTicketCount(int number)
    {
        lock (_lock)
        {
            TicketCount = number;
        }
        SomethingInterestingIsChanging();
    }

    internal void TogglePause()
    {
        lock (_lock)
        {
            IsPaused = !IsPaused;
        }
        SomethingInterestingIsChanging();
    }

    internal void ShowQRCode()
    {
        lock (_lock)
        {
            ShowQR = !ShowQR;
        }
        SomethingInterestingIsChanging();
    }

    internal void ToggleMute()
    {
        lock (_lock)
        {
            IsMuted = !IsMuted;
        }
        SomethingInterestingIsChanging();
    }

    public void ReadAloud(int tableId, bool useNaturalVoice)
    {
        if (IsMuted) return;

        TanNumber? x;
        lock (_lock)
        {
            var list = GetTableList(tableId);
            x = list.FirstOrDefault();
        }

        if (x is not null && x.CardNumber > 0)
        {
            string textToRead = $"Now Serving {x.CardNumber} on Table {tableId}";
            _ss.ReadAloud(textToRead, useNaturalVoice);
        }
    }

    internal void SetWorkerCount(int count)
    {
        if (count <= 0) return;
        lock (_lock)
        {
            WorkerCount = count;
        }
        SomethingInterestingIsChanging();
    }

    public event EventHandler HeySomethingChanged;
    public void SomethingInterestingIsChanging()
    {
        HeySomethingChanged?.Invoke(this, EventArgs.Empty);
    }
}
public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid(); 
    public string Sender { get; set; }
    public string Text { get; set; }
    public DateTime Timestamp { get; set; }
    public bool Edited { get; set; }
}
