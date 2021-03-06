﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattlePrep : MonoBehaviour {

    /// <summary>
    /// Text displaying party stats during the Battle Prep stage
    /// </summary>
    public Text statText;

    /// <summary>
    /// Image which appears and disappears based on if the player is hovering over a party selection image or not
    /// </summary>
    public Image statView;

    /// <summary>
    /// The BattleManager for this particular fight
    /// </summary>
    public BattleManager bm;

    /// <summary>
    /// A list of all the character selection images in the battle prep window
    /// </summary>
    public CharacterImage[] ci;

    /// <summary>
    /// The text which tells the player how many characters they can choose for the battle
    /// </summary>
    public Text remainingText;

    //The active, selected party
    private List<Entity> battlingParty = new List<Entity>();

    //The number of party memebers allowed
    private int numMembers;

	void Start () {

        numMembers = bm.GetNumMembers();
        FullParty fp = FindObjectOfType<FullParty>();

        foreach(CharacterImage c in ci)
        {
            foreach(PartyMember p in fp.GetParty())
            {
                if (c.nameOfChar == p.Name)
                {
                    c.SetCharacter(p); //Set character to correct selection image
                }
            }

            //PartyMember not present. Hide selection image
            if (c.GetCharacter() == null) c.gameObject.SetActive(false);
        }

        remainingText.text = "Select " + numMembers + " More";
	}

    /*
    void Update()
    {
        statView.gameObject.SetActive(false);

        foreach (CharacterImage c in ci)
        {
            if (c.IsHovering())
            {
                statView.gameObject.SetActive(true);
                statText.text = c.GetCharacter().GetAllStats();
            }
        }
    }
    */

    /// <summary>
    /// Add a PC to party which will partake in the battle after selecting them
    /// </summary>
    /// <param name="p">The PartyMember to be added</param>
    public void AddToParty(PartyMember p)
    {
        battlingParty.Add(p);

        remainingText.text = "Select " + (numMembers - battlingParty.Count) + " More";
    }

    /// <summary>
    /// Remove a PC from the party which will partake in the battle after deselecting them
    /// </summary>
    /// <param name="p">The PartyMember to be removed</param>
    public void RemoveFromParty(PartyMember p)
    {
        battlingParty.Remove(p);

        remainingText.text = "Select " + (numMembers - battlingParty.Count) + " More";
    }

    /// <summary>
    /// Begin the battle with the chosen party after selection is complete. Makes a call to the BattleManager.
    /// </summary>
    public void StartBattle()
    {
        if (battlingParty.Count == 0) return;

        bm.InstantiatePlayerParty(battlingParty);
        bm.SetState("NORMAL");
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Determines if the number of party members chosen has hit the battle limit
    /// </summary>
    /// <returns>True - the player cannot select anymore party members; False - the player can select at least 1 more </returns>
    public bool IsFull()
    {
        if (battlingParty.Count == numMembers) return true;
        else return false;
    }

}
