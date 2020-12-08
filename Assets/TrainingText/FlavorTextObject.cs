using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using KModkit;

public class FlavorTextObject {
    private string moduleName;
    private int year;
    private int month;
    private int day;
    private string flavorText;

    private bool hasQuotes = false;
    private bool startsDP = false;

    private readonly char[] validLetters = { 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P' };

    public FlavorTextObject() {
        moduleName = "Wires";
        year = 2015;
        month = 10;
        day = 8;
        flavorText = "Cut all the things. No, wait, cut only what is needed.";
    }

    public FlavorTextObject(string moduleName, int year, int month, int day, string flavorText) {
        this.moduleName = moduleName;
        this.year = year;
        this.month = month;
        this.day = day;
        this.flavorText = flavorText;

        if (flavorText.Contains("\""))
            hasQuotes = true;

        startsDP = checkStartsDP(this.moduleName);
    }


    public string getModuleName() {
        return moduleName;
    }

    public int getYear() {
        return year;
    }

    public int getMonth() {
        return month;
    }

    public int getDay() {
        return day;
    }

    public string getFlavorText() {
        return flavorText;
    }

    public bool getHasQuotes() {
        return hasQuotes;
    }

    public bool getStartsDP() {
        return startsDP;
    }


    // Determines if the module name starts with D-p
    public bool checkStartsDP(string txt) {
        // Removes the word "needy"
        if (txt.Length > 6 && txt.Substring(0, 6) == "Needy ")
            txt = txt.Substring(6, txt.Length - 6);

        for (int i = 0; i < validLetters.Length; i++) {
            if (txt.First() == validLetters[i])
                return true;
        }

        return false;
    }
}