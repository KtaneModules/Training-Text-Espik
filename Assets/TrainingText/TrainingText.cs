using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using KModkit;
using System.Text.RegularExpressions;

public class TrainingText : MonoBehaviour {
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;

    public KMSelectable SubmitButton;
    public KMSelectable[] TimeButtons;

    public Text FlavorText;
    public TextMesh ClockText;
    public TextMesh AnswerText;

    public TextMesh[] ClockNumbers;
    public Renderer[] ClockTickmarks;
    public Color[] ClockColors;
    public Material[] ClockMaterials;

    // Logging info
    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved = false;

    // Solving info
    private int lastSerialDigit;
    private int batteryCount;
    private bool hasSerialPort = false;
    private bool hasEmptyPlate = false;

    private FlavorTextList flavorTexts;
    private FlavorTextObject module = new FlavorTextObject();

    private bool displayingModuleName = false;

    private int correctTime = 0;
    private int currentTime = 0;
    private int realTime = 0;

    // Ran as bomb loads
    private void Awake() {
        moduleId = moduleIdCounter++;

        SubmitButton.OnInteract += delegate () { SubmitButtonPressed(); return false; };

        for (int i = 0; i < TimeButtons.Length; i++) {
            int j = i;
            TimeButtons[i].OnInteract += delegate () { TimeButtonPressed(j); return false; };
        }
    }

    // Gets information
    private void Start() {
        // Gets edgework and day of the week
        lastSerialDigit = Bomb.GetSerialNumberNumbers().Last();
        batteryCount = Bomb.GetBatteryCount();

        if (Bomb.GetPortCount(Port.Serial) > 0)
            hasSerialPort = true;

        foreach (object[] plate in Bomb.GetPortPlates()) {
            if (plate.Length == 0) {
                hasEmptyPlate = true;
                break;
            }
        }


        // Gets the list of flavor texts and chooses a random text
        flavorTexts = AllFlavorTexts.AddFlavorTexts();
        module = flavorTexts.getRandomFlavorText();

        // Formats the flavor text for logging
        string modifiedFlavorText = module.getFlavorText();
        modifiedFlavorText = modifiedFlavorText.Replace('\n', ' ');

        Debug.LogFormat("[Training Text #{0}] The module selected was {1}.", moduleId, module.getModuleName());
        Debug.LogFormat("[Training Text #{0}] The flavor text is: {1}", moduleId, modifiedFlavorText);
        Debug.LogFormat("[Training Text #{0}] The module was released on {1}/{2}/{3}.", moduleId, module.getMonth(), module.getDay(), module.getYear());


        // Sets a random time on the clock
        currentTime = UnityEngine.Random.Range(0, 1440);

        CalculateCorrectTime();
        DisplayCurrentTime();

        FlavorText.text = module.getFlavorText();
    }


    // Formats the time
    private string FormatTime(int time) {
        string hour, minute, state;

        if (time >= 720) {
            time -= 720;
            state = "PM";
        }

        else
            state = "AM";

        int hours = 0;
        while (time >= 60) {
            hours++;
            time -= 60;
        }

        hour = hours.ToString();
        if (hour == "0")
            hour = "12";

        if (time < 10)
            minute = "0" + time.ToString();

        else
            minute = time.ToString();

        return hour + ":" + minute + " " + state;
    }

    // Modifies the time to stay in bounds
    private int ModifyTime(int time) {
        while (time < 0)
            time += 1440;

        return time % 1440;
    }
    

    // Calculates correct time
    private void CalculateCorrectTime() {
        correctTime += module.getMonth() % 12 * 60;
        correctTime += module.getDay();

        if (lastSerialDigit % 2 == 0)
            correctTime += 720;

        Debug.LogFormat("[Training Text #{0}] The unmodified time is {1}", moduleId, FormatTime(correctTime));


        // Modifies the time
        bool rulesApplied = false;

        if (module.getYear() < 2017) {
            correctTime = ModifyTime(correctTime + 45);
            rulesApplied = true;
            Debug.LogFormat("[Training Text #{0}] The module was released before 2017. (+45 minutes)", moduleId);
        }

        if (module.getHasQuotes() == true) {
            correctTime = ModifyTime(correctTime + 20);
            rulesApplied = true;
            Debug.LogFormat("[Training Text #{0}] The module's flavor text has quotation marks. (+20 minutes)", moduleId);
        }

        if (module.getStartsDP() == true) {
            correctTime = ModifyTime(correctTime - 30);
            rulesApplied = true;
            Debug.LogFormat("[Training Text #{0}] The module's name starts with a letter between D and P. (-30 minutes)", moduleId);
        }

        if (module.getMonth() == 1) {
            correctTime = ModifyTime(correctTime - 300);
            rulesApplied = true;
            Debug.LogFormat("[Training Text #{0}] The module was released in January. (-5 hours)", moduleId);
        }

        if (Bomb.GetSolvableModuleNames().Count(x => x.Contains("Training Text")) > 1) {
            correctTime = ModifyTime(correctTime + 60);
            rulesApplied = true;
            Debug.LogFormat("[Training Text #{0}] There is another Training Text module on the bomb. (+1 hour)", moduleId);
        }

        if (hasSerialPort == true) {
            correctTime = ModifyTime(correctTime + 5);
            rulesApplied = true;
            Debug.LogFormat("[Training Text #{0}] The bomb has a serial port. (+5 minutes)", moduleId);
        }

        if (hasEmptyPlate == true) {
            correctTime = ModifyTime(correctTime - 90);
            rulesApplied = true;
            Debug.LogFormat("[Training Text #{0}] The bomb has an empty port plate. (-90 minutes)", moduleId);
        }

        if (batteryCount == 0) {
            correctTime = ModifyTime(correctTime - 10);
            rulesApplied = true;
            Debug.LogFormat("[Training Text #{0}] The bomb has no batteries. (-10 minutes)", moduleId);
        }

        if (rulesApplied == false)
            Debug.LogFormat("[Training Text #{0}] No rules from Step 3 applied.", moduleId);

        else
            Debug.LogFormat("[Training Text #{0}] The time after Step 3 is {1}", moduleId, FormatTime(correctTime));

        // If the specified module is on the bomb
        if (Bomb.GetSolvableModuleNames().Contains(module.getModuleName())) {
            Debug.LogFormat("[Training Text #{0}] The module selected is present on the bomb.", moduleId);

            int currentState = currentTime / 720;
            int currentHour = currentTime % 720 / 60;
            int currentMinute = currentTime % 60;

            currentState = (currentState + 1) % 2;
            currentHour = (currentHour + 6) % 12;
            currentMinute = (currentMinute + 30) % 60;

            correctTime = currentState * 720 + currentHour * 60 + currentMinute;
        }

        Debug.LogFormat("[Training Text #{0}] The correct time to submit is {1}", moduleId, FormatTime(correctTime));
    }


    // Displays time and highlights tickmarks
    private void DisplayCurrentTime() {
        ClockText.text = FormatTime(currentTime);

        int currentHour = currentTime % 720 / 60;
        int currentMinute = currentTime % 60;

        ClockNumbers[currentHour].color = ClockColors[1];

        if (currentHour == 0) {
            ClockNumbers[11].color = ClockColors[0];
            ClockNumbers[1].color = ClockColors[0];
        }

        else {
            ClockNumbers[(currentHour - 1) % 12].color = ClockColors[0];
            ClockNumbers[(currentHour + 1) % 12].color = ClockColors[0];
        }

        ClockTickmarks[currentMinute].material = ClockMaterials[1];

        if (currentMinute == 0) {
            ClockTickmarks[59].material = ClockMaterials[0];
            ClockTickmarks[1].material = ClockMaterials[0];
        }

        else {
            ClockTickmarks[(currentMinute - 1) % 60].material = ClockMaterials[0];
            ClockTickmarks[(currentMinute + 1) % 60].material = ClockMaterials[0];
        }
    }


    // Submit button pressed
    private void SubmitButtonPressed() {
        SubmitButton.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, SubmitButton.transform);
        bool correct = false;

        if (moduleSolved == false) {
            Debug.LogFormat("[Training Text #{0}] You submitted {1}", moduleId, FormatTime(currentTime));

            // If the correct time is within an hour of the real time
            if (DateTime.Now.DayOfWeek.ToString() != "Friday") {
                // Gets the real time
                realTime = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
                Debug.LogFormat("[Training Text #{0}] The current local time when pressing the button was {1}", moduleId, FormatTime(realTime));

                // Compares the time
                if ((correctTime >= realTime - 60 && correctTime <= realTime + 60) ||
                    (correctTime + 1440 >= realTime - 60 && correctTime + 1440 <= realTime + 60) ||
                    (correctTime - 1440 >= realTime - 60 && correctTime - 1440 <= realTime + 60))
                    correctTime = realTime;
            }

            else
                Debug.LogFormat("[Training Text #{0}] The answer was submitted on a Friday.", moduleId);


            // Correct answer
            if (currentTime == correctTime) {
                Debug.LogFormat("[Training Text #{0}] Module solved!", moduleId);
                GetComponent<KMBombModule>().HandlePass();
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, gameObject.transform);
                moduleSolved = true;
                correct = true;
            }

            // Incorrect answer
            else {
                Debug.LogFormat("[Training Text #{0}] Strike!", moduleId);
                GetComponent<KMBombModule>().HandleStrike();
            }
        }

        // Displays the module name on the top screen
        if (displayingModuleName == false) {
            displayingModuleName = true;

            if (correct == true)
                AnswerText.text = module.getModuleName();

            else
                StartCoroutine(ModuleNameFlash());
        }
    }

    // Displays the module name upon striking
    private IEnumerator ModuleNameFlash() {
        yield return new WaitForSeconds(0.6f);
        Audio.PlaySoundAtTransform("TrainingText_TextShow", transform);
        yield return new WaitForSeconds(0.04f);
        AnswerText.text = module.getModuleName();
        yield return new WaitForSeconds(0.25f);
        AnswerText.text = "";
        yield return new WaitForSeconds(0.15f);
        AnswerText.text = module.getModuleName();
        yield return new WaitForSeconds(0.25f);
        AnswerText.text = "";
        yield return new WaitForSeconds(0.15f);
        AnswerText.text = module.getModuleName();
    }

    // Time button pressed
    private void TimeButtonPressed(int i) {
        TimeButtons[i].AddInteractionPunch(0.25f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, TimeButtons[i].transform);
        if (moduleSolved == false) {
            /* 0 = Hour Up
             * 1 = Hour Down
             * 2 = Minute Up
             * 3 = Minute Down
             */

            switch (i) {
            case 0: currentTime = ModifyTime(currentTime + 60); break;
            case 1: currentTime = ModifyTime(currentTime - 60); break;
            case 2: currentTime = ModifyTime(currentTime + 1); break;
            case 3: currentTime = ModifyTime(currentTime - 1); break;
            }

            DisplayCurrentTime();
        }
    }
    
    // Twitch Plays Support - Thanks to eXish & kavinkul

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} hours/minutes <forward/backward> <#> [Adjusts the hours or minutes forward or backward on the clock by '#' (Hours and minutes will be modulo by 24 and 60 respectively)] | !{0} set <#:##/##:##> <AM/PM> [Sets the specified time in #:## or ##:## format to AM or PM on the clock and submits it] | !{0} submit [Submits the current time on the clock]";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command) {
        command = command.ToLowerInvariant().Trim();
        Match m = Regex.Match(command, @"^(?:submit|(hours|minutes) (forward|backward) (\d{1,2})|set (\d{1,2}):(\d{2}) (a|p)m)$");
        if (m.Success) {
            if (m.Groups[3].Success) {
                yield return null;
                KMSelectable button;
                button = m.Groups[1].Value == "hours" ? m.Groups[2].Value == "forward" ? TimeButtons[0] : TimeButtons[1] : m.Groups[2].Value == "forward" ? TimeButtons[2] : TimeButtons[3];
                int count = m.Groups[1].Value == "hours" ? int.Parse(m.Groups[3].Value) % 24 : int.Parse(m.Groups[3].Value) % 60;
                for (int i = 0; i < count; i++) {
                    button.OnInteract();
                    yield return new WaitForSeconds(.05f);
                    yield return "trycancel";
                }
            }
            else if (m.Groups[4].Success) {
                int tpHours = int.Parse(m.Groups[4].Value);
                int tpMins = int.Parse(m.Groups[5].Value);
                if (tpHours < 1 || tpHours > 12 || tpMins < 0 || tpMins > 59) {
                    yield return "sendtochaterror Invalid time! Hours must be in between 1 - 12 and minutes must be in between 0 - 59";
                    yield break;
                }
                yield return null;
                int minutesDiff = Math.Abs((currentTime % 60) - tpMins);
                KMSelectable button;
                button = (currentTime % 60) > tpMins ? minutesDiff > 30 ? TimeButtons[2] : TimeButtons[3] : minutesDiff > 30 ? TimeButtons[3] : TimeButtons[2];
                while ((currentTime % 60) != tpMins) {
                    button.OnInteract();
                    yield return new WaitForSeconds(.05f);
                    yield return "trycancel";
                }
                int AMPMOffset = currentTime > 719 ? 12 : 0;
                int current24Hour = AMPMOffset + currentTime / 60 % 12;
                int target24Hour = (tpHours % 12) + (m.Groups[6].Value == "p" ? 12 : 0);
                int hoursDiff = Math.Abs(current24Hour - target24Hour);
                button = current24Hour > target24Hour ? hoursDiff > 12 ? TimeButtons[0] : TimeButtons[1] : hoursDiff > 12 ? TimeButtons[1] : TimeButtons[0];
                while (current24Hour != target24Hour) {
                    button.OnInteract();
                    yield return new WaitForSeconds(.05f);
                    yield return "trycancel";
                    AMPMOffset = currentTime > 719 ? 12 : 0;
                    current24Hour = AMPMOffset + currentTime / 60 % 12;
                }
            }
            else {
                yield return null;
                SubmitButton.OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }
        else
            yield return "sendtochaterror Invalid command! Please use !{1} help to see full command.";
        yield break;
    }

    // TP Autosolver variables
    private bool answerIsCorrect;
    IEnumerator TwitchHandleForcedSolve() {
        do {
            answerIsCorrect = true;
            int temptime = correctTime;
            string hourToSubmit = (correctTime / 60) == 0 ? "12" : (correctTime / 60).ToString();
            if (int.Parse(hourToSubmit) > 12)
                hourToSubmit = (int.Parse(hourToSubmit) - 12).ToString();
            string minuteToSubmit = (correctTime % 60).ToString();
            if (minuteToSubmit.Length != 2)
                minuteToSubmit = "0" + minuteToSubmit;
            yield return ProcessTwitchCommand("set " + hourToSubmit + ":" + minuteToSubmit + " " + ((correctTime > 719) ? "PM" : "AM"));

            // Due to length of input, check again if the answer is right.
            if (DateTime.Now.DayOfWeek.ToString() != "Friday")
            {
                realTime = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
                if ((correctTime >= realTime - 60 && correctTime <= realTime + 60) ||
                    (correctTime + 1440 >= realTime - 60 && correctTime + 1440 <= realTime + 60) ||
                    (correctTime - 1440 >= realTime - 60 && correctTime - 1440 <= realTime + 60))
                    correctTime = realTime;
            }
            if (temptime != correctTime)
                answerIsCorrect = false;
        }
        while (!answerIsCorrect);
        SubmitButton.OnInteract();
        yield return new WaitForSeconds(.1f);
    }
}