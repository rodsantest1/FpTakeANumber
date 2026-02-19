namespace FpTakeANumberApp1;

using Microsoft.CognitiveServices.Speech;
using old = System.Speech.Synthesis;
using static System.Net.Mime.MediaTypeNames;


public class SpeechService
{
    private readonly old.SpeechSynthesizer _synth = new();
    private readonly object _lock = new();

    // work item https://rodcharowner.visualstudio.com/CollectiveKnowlege/_workitems/edit/91/
    private readonly string _subscriptionKey = "G7H1K1XYTASFl9y3flztRLnSaljBKCyFx0mllSJfoBubfLhpw22pJQQJ99BKACHYHv6XJ3w3AAAYACOGsY9P";

    private readonly string _region = "eastus2";

    private readonly SemaphoreSlim _speechLock = new(1, 1);

    public async Task ReadAloud(string textToRead, bool useNaturalVoice = true)
    {
        //var reader = await _appSettingsService.GetEffectiveReaderAsync();
        if (useNaturalVoice)
        {
            await ReadAloudNew(textToRead);
            Console.WriteLine("Using new speech synthesis.");
        }
        else
        {
            await ReadAloudOld(textToRead);
            Console.WriteLine("Using old speech synthesis.");
        }
    }

    public async Task ReadAloudOld(string textToRead)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                _synth.SelectVoice("Microsoft Zira Desktop");
                _synth.Rate = -1;
                _synth.Speak(textToRead);
            }
        });
    }

    public async Task ReadAloudNew(string textToRead)
    {
        await _speechLock.WaitAsync();
        try
        {
            var config = SpeechConfig.FromSubscription(_subscriptionKey, _region);
            config.SpeechSynthesisVoiceName = "en-US-NancyMultilingualNeural";

            using var synthesizer = new Microsoft.CognitiveServices.Speech.SpeechSynthesizer(config);

            var result = await synthesizer.SpeakTextAsync(textToRead);

            if (result.Reason != ResultReason.SynthesizingAudioCompleted)
            {
                Console.WriteLine($"Speech synthesis failed: {result.Reason}");
                Console.WriteLine($"Error details: {result}");
            }
        }
        finally
        {
            _speechLock.Release();
        }
    }
}
