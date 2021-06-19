using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class AccessCodesScript : MonoBehaviour {

    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBossModule Boss;
    public MeshRenderer[] ButtonColours;
    public MeshRenderer BGColour;
    public KMSelectable[] Buttons;
    public TextMesh Display;
    public static string[] IgnoredModules = null;

    //public double PublicVoltage;

    private int[][] Grid = { new int[] { 2, 5, 3, 4, 0, 1, 5, 1, 0, 2 }, new int[] { 4, 3, 3, 2, 4, 0, 1, 5, 4, 3 }, new int[] { 2, 1, 5, 0, 0, 4, 1, 5, 3, 2 }, new int[] { 1, 0, 5, 3, 2, 4 } };
    private int[] SerialBase36 = new int[6];
    private int SolvedCache;
    private string Base36Ref = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private string LettersRef = "ABCDEF";
    private string CurrentInput = "";
    private string Solution = "ABCDEF";
    private bool Solved;
    private bool TPAutosolve;

    private double Voltage()
    {
        if (Bomb.QueryWidgets("volt", "").Count() != 0)
        {
            double TempVoltage = double.Parse(Bomb.QueryWidgets("volt", "")[0].Substring(12).Replace("\"}", ""));
            return TempVoltage;
            //return PublicVoltage;
        }
        return -1d;
        //return -PublicVoltage;
    }

    class VoltageRule
    {
        public double Voltage;
        public int[] Sum;

        public VoltageRule(double voltage, IEnumerable<int> sum)
        {
            Voltage = voltage;
            Sum = sum.ToArray();
        }
    }

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        if (IgnoredModules == null)
            IgnoredModules = GetComponent<KMBossModule>().GetIgnoredModules("Access Codes", new string[]{
                "14",
                "Access Codes",
                "Forget Enigma",
                "Forget Everything",
                "Forget It Not",
                "Forget Me Later",
                "Forget Me Not",
                "Forget Perspective",
                "Forget Them All",
                "Forget This",
                "Forget Us Not",
                "Organization",
                "Purgatory",
                "Simon's Stages",
                "Souvenir",
                "Tallordered Keys",
                "The Time Keeper",
                "Timing is Everything",
                "The Troll",
                "Turn The Key",
                "Übermodule",
                "Ültimate Custom Night",
                "The Very Annoying Button"
            });
        BGColour.material.color = Rnd.ColorHSV(0f, 1f, 0.2f, 0.2f, 0.5f, 0.5f);
        for (int i = 0; i < ButtonColours.Length; i++)
        {
            ButtonColours[i].material.color = BGColour.material.color;
        }
        for (int i = 0; i < 8; i++)
        {
            int x = i;
            Buttons[i].OnInteract += delegate { StartCoroutine(ButtonPress(x)); return false; };
        }
        Display.text = "";
        Module.OnActivate += delegate { Audio.PlaySoundAtTransform("activate", Display.transform); };
    }

    // Use this for initialization
    void Start () {
        Calculate();
    }

    // Update is called once per frame
    void Update () {
        if (Bomb.GetSolvedModuleNames().Count() != SolvedCache)
            SolvedCheck();
	}

    void SolvedCheck()
    {
        SolvedCache = Bomb.GetSolvedModuleNames().Count();
        if (!IgnoredModules.Contains(Bomb.GetSolvedModuleNames().Last()) && !Solved && !TPAutosolve)
        {
            Module.HandleStrike();
            Audio.PlaySoundAtTransform("strike", Display.transform);
            Debug.LogFormat("[Access Codes #{0}] {1} was solved before this module. Strike!", _moduleID, Bomb.GetSolvedModuleNames().Last());
        }
    }

    void Calculate()
    {
        Solution = "";
        for (int i = 0; i < 6; i++)
        {
            SerialBase36[i] = Base36Ref.IndexOf(Bomb.GetSerialNumber()[i]);
        }
        Debug.LogFormat("[Access Codes #{0}] The serial number converted from base 36 to decimal is {1}.", _moduleID, SerialBase36.Join(", "));
        if (Bomb.GetPorts().Any(p => !new[] { "Parallel", "Serial", "DVI", "PS2", "StereoRCA", "RJ45" }.Contains(p)) && Bomb.GetPortCount() != 0)
        {
            Debug.LogFormat("[Access Codes #{0}] Found modded ports on the bomb.", _moduleID);
            foreach (var port in Bomb.GetPorts())
            {
                if (!new[] { "Parallel", "Serial", "DVI", "PS2", "StereoRCA", "RJ45" }.Contains(port))
                {
                    for (int i = 0; i < 6; i++)
                    {
                        switch (port)
                        {
                            case "CompositeVideo":
                                SerialBase36[i]++;
                                break;
                            case "VGA":
                                SerialBase36[i] = SerialBase36[i] + 2;
                                break;
                            case "ComponentVideo":
                                SerialBase36[i] = SerialBase36[i] + 3;
                                break;
                            case "AC":
                                SerialBase36[i] = SerialBase36[i] + 4;
                                break;
                            case "PCMCIA":
                                SerialBase36[i] = SerialBase36[i] + 5;
                                break;
                            case "USB":
                                SerialBase36[i] = SerialBase36[i] + 6;
                                break;
                            default:
                                SerialBase36[i] = SerialBase36[i] + 7;
                                break;
                        }
                    }
                }
            }
            Debug.LogFormat("[Access Codes #{0}] The modified numbers are {1}.", _moduleID, SerialBase36.Join(", "));
            for (int i = 0; i < 6; i++)
                Solution += LettersRef[SerialBase36[i] % 6];
        }
        else if (Voltage() != -1d)
        {
            Debug.LogFormat("[Access Codes #{0}] No modded ports found. However, the bomb's voltage is known which is {1} volts.", _moduleID, Voltage());
            int[] TempSerialBase36 = new int[6];
            for (int i = 0; i < 6; i++)
            {
                TempSerialBase36[i] = SerialBase36[i];
            }
            List<VoltageRule> Ruleset = new List<VoltageRule> {
            new VoltageRule(1d, SerialBase36.Select(n => n + TempSerialBase36.Sum())),
            new VoltageRule(1.5d, SerialBase36.Select(n => n + TempSerialBase36.Where(x => x % 2 == 1).Sum())),
            new VoltageRule(2d, SerialBase36.Select(n => n + TempSerialBase36.Where(x => x % 2 == 0).Sum())),
            new VoltageRule(2.5d, SerialBase36.Select(n => n + TempSerialBase36[0] + TempSerialBase36[2] + TempSerialBase36[4])),
            new VoltageRule(3d, SerialBase36.Select(n => n + TempSerialBase36[1] + TempSerialBase36[3] + TempSerialBase36[5])),
            new VoltageRule(3.5d, SerialBase36.Select(n => n + TempSerialBase36[0] + TempSerialBase36[1] + TempSerialBase36[2])),
            new VoltageRule(4d, SerialBase36.Select(n => n + TempSerialBase36[3] + TempSerialBase36[4] + TempSerialBase36[5])),
            new VoltageRule(4.5d, SerialBase36.Select(n => n + TempSerialBase36[0] + TempSerialBase36[5])),
            new VoltageRule(5d, SerialBase36.Select(n => n + TempSerialBase36[1] + TempSerialBase36[4])),
            new VoltageRule(5.5d, SerialBase36.Select(n => n + TempSerialBase36[2] + TempSerialBase36[3])),
            new VoltageRule(6d, SerialBase36.Select(n => n + TempSerialBase36.Where(x => x < 13).Sum())),
            new VoltageRule(6.5d, SerialBase36.Select(n => n + TempSerialBase36.Where(x => x >= 13).Sum())),
            new VoltageRule(7d, SerialBase36.Select(n => n + TempSerialBase36[1] + TempSerialBase36[2] + TempSerialBase36[4])),
            new VoltageRule(7.5d, SerialBase36.Select(n => n + (TempSerialBase36[0]) * 6)),
            new VoltageRule(8d, SerialBase36.Select(n => n + (TempSerialBase36[1]) * 6)),
            new VoltageRule(8.5d, SerialBase36.Select(n => n + (TempSerialBase36[2]) * 6)),
            new VoltageRule(9d, SerialBase36.Select(n => n + (TempSerialBase36[3]) * 6)),
            new VoltageRule(9.5d, SerialBase36.Select(n => n + (TempSerialBase36[4]) * 6)),
            new VoltageRule(10d, SerialBase36.Select(n => n + (TempSerialBase36[5]) * 6))
                // etc.
            };
            VoltageRule FirstApplicableRule = Ruleset.Find(x => x.Voltage == Voltage());
            SerialBase36 = FirstApplicableRule.Sum;
            Debug.LogFormat("[Access Codes #{0}] The modified numbers are {1}.", _moduleID, SerialBase36.Join(", "));
            for (int i = 0; i < 6; i++)
                Solution += LettersRef[SerialBase36[i] % 6];
        }
        else
        {
            Debug.LogFormat("[Access Codes #{0}] No modded ports found and the bomb's voltage is not known.", _moduleID);
            for (int i = 0; i < 6; i++)
            {
                int Row = int.Parse(SerialBase36[i].ToString("00")[0].ToString());
                int Column = int.Parse(SerialBase36[i].ToString("00")[1].ToString());
                int GridModifier = 0;
                for (int j = 0; j < i; j++)
                {
                    switch (Solution[j])
                    {
                        case 'A':
                            if (Row == 3)
                                Column = (Column + 1) % 6;
                            else
                                Column = (Column + 1) % 10;
                            break;
                        case 'B':
                            if (Column > 5)
                                Row = (Row + 2) % 3;
                            else
                                Row = (Row + 3) % 4;
                            break;
                        case 'C':
                            if (Column > 5)
                                Row = (Row + 1) % 3;
                            else
                                Row = (Row + 1) % 4;
                            break;
                        case 'D':
                            if (Row == 3)
                                Column = (Column + 5) % 6;
                            else
                                Column = (Column + 9) % 10;
                            break;
                        case 'E':
                            GridModifier++;
                            break;
                        default:
                            GridModifier = GridModifier + 5;
                            break;
                    }
                }
                Solution += LettersRef[(Grid[Row][Column] + GridModifier) % 6];
            }
        }
        Debug.LogFormat("[Access Codes #{0}] The solution is {1}.", _moduleID, Solution);
    }

    private IEnumerator ButtonPress(int pos)
    {
        Audio.PlaySoundAtTransform("button press", Buttons[pos].transform);
        Buttons[pos].AddInteractionPunch();
        for (int i = 0; i < 3; i++)
        {
            Buttons[pos].transform.localPosition -= new Vector3(0, 0.01f / 3, 0);
            yield return null;
        }
        if (!Solved)
        {
            if (pos < 6 && CurrentInput.Length < 6)
            {
                CurrentInput += Buttons[pos].GetComponentInChildren<TextMesh>().text;
                Display.text = CurrentInput;
            }
            else if (pos == 6)
            {
                CurrentInput = "";
                Display.text = "";
            }
            else if (pos == 7)
            {
                if (CurrentInput == Solution)
                {
                    Module.HandlePass();
                    Solved = true;
                    Audio.PlaySoundAtTransform("solve", Display.transform);
                    Display.text = "SOLVED";
                    Debug.LogFormat("[Access Codes #{0}] You input \"{1}\", which was correct. Module solved!", _moduleID, CurrentInput);
                }
                else
                {
                    Module.HandleStrike();
                    Audio.PlaySoundAtTransform("strike", Display.transform);
                    Debug.LogFormat("[Access Codes #{0}] You input \"{1}\", which was incorrect. Strike!", _moduleID, CurrentInput);
                }
            }
        }
        for (int i = 0; i < 3; i++)
        {
            Buttons[pos].transform.localPosition += new Vector3(0, 0.01f / 3, 0);
            yield return null;
        }
    }
#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} ABCDEF' to type ABCDEF and '!{0} reset/submit' to press either the reset or submit button.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToUpperInvariant();
        string validcmds = "ABCDEF";
        for (int i = 0; i < command.Length; i++)
        {
            if (!validcmds.Contains(command[i]))
                if (command != "RESET" && command != "SUBMIT")
                {
                    yield return "sendtochaterror Invalid command.";
                    yield break;
                }
        }
        yield return null;
        if (command == "SUBMIT")
        {
            Buttons[7].OnInteract();
        }
        else if (command == "RESET")
        {
            Buttons[6].OnInteract();
        }
        else
        {
            for (int i = 0; i < command.Length; i++)
            {
                Buttons[LettersRef.IndexOf(command[i])].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
    IEnumerator TwitchHandleForcedSolve()
    {
        TPAutosolve = true;
        yield return null;
        for (int i = 0; i < 6; i++)
        {
            Buttons[LettersRef.IndexOf(Solution[i])].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        Buttons[7].OnInteract();
    }
}
