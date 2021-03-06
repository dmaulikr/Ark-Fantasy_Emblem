﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// An essential class containing all the basic information for player characters and enemies alike
/// </summary>
public class Entity : MonoBehaviour {

    /// <summary>Name that will be displayed in battle and stat screens</summary>
    public string Name;

    /// <summary>
    /// Amount of experience points to award when this entity is defeated
    /// </summary>
    public int expGain;

    //The ID number of this entity in its party
    private int index;

    protected bool hovering = false; //If the mouse if hovering over the entity or not
    protected Party party; //The party this entity belongs to (player or enemy)

    /***STATS***/
    public int level = 1;

    //Assistant classes
    [HideInInspector] public BattleCalculator bc;
    [HideInInspector] public EffectCalculator ec;
    [HideInInspector] public PositionCalculator pc;

    /// <summary> Organic, Magic, or Droid - can be multityped </summary>
    public enum TYPE
    {
        ORGANIC, MAGIC, DROID, ORGANIC_MAGIC, DROID_ORGANIC, MAGIC_DROID
    }
    public TYPE type;

    /// <summary> Hit Points </summary>
    public int maxHP; protected int _hp; //Health

    //OFFENSIVE STATS
    /// <summary> Physical strength </summary>
    public int baseAtk; protected int _atk;
    /// <summary> Magical strength </summary>
    public int baseMag; protected int _mag;
    /// <summary> Electrical strength </summary>
    public int baseVlt; protected int _vlt;

    //DEFENSIVE STATS
    /// <summary> Physical resistance </summary>
    public int baseDef; protected int _def;
    /// <summary> Magical resistance </summary>
    public int baseRes; protected int _res;
    /// <summary> Electrical resistance </summary>
    public int baseStb; protected int _stb;

    //PERFORMANCE STATS
    /// <summary> Determines critical hit rate </summary>
    public int baseSkl; protected int _skl;
    /// <summary> Lowers the chance of being hit by a critical </summary>
    public int baseLck; protected int _lck;
    /// <summary> Determines how fast the speed gauge increases </summary>
    public int baseSpd; protected int _spd;

    //SPECIALS
    public List<Special> skills;
    public List<Special> spells;
    public List<Special> techs;

    /// <summary> Enum which keeps track of player statuses such as death or negative status effects</summary>
    [HideInInspector]
    public enum STATUS
    {
        NORMAL, ILL, DEFENDING, DEAD
    }

    /// <summary> Status variable </summary>
    [HideInInspector]
    public STATUS status = STATUS.NORMAL;

    //Target Color
    protected Color targeted = Color.red;
    protected Color active = Color.green;
    protected Color normal;
    protected Color hover = Color.gray;

    //Speed
    protected float moveTimer = 0f; //Counts up to 100 over time, and then the entity can act
    protected bool ready = false; //True if moveTimer = 100, false otherwise

    //Components
    protected Animator anim; //The animator used for battle animations
    protected SpriteRenderer render; //Renders sprites
    protected AnimationClip[] clips; //Animator clips

    //STAT DISPLAY
    /// <summary>The canvas containg relevant states, namely HP and speed progress bar</summary>
    public Canvas overhead;
    /// <summary>HP display</summary>
    public Image hpBar;
    /// <summary>The textbox which displays the index number of this entity in its party</summary>
    public Text indexText;
    /// <summary>Length of speed and health bars</summary>
    public float barsLength;
    /// <summary>Height of speed and health bars</summary>
    public float barsHeight;

    //LEVELING UP
    private int _exp = 0;

    public int l_hp;

    public int l_atk;
    public int l_mag;
    public int l_vlt;

    public int l_def;
    public int l_res;
    public int l_stb;

    public int l_skl;
    public int l_lck;
    public int l_spd;

    //Modifers
    private float speedMultiplier = 5f; //Basic multiplier to speed up or slow down all combat

    protected virtual void Awake()
    {
        anim = GetComponent<Animator>();
        render = GetComponent<SpriteRenderer>();

        pc = new PositionCalculator(this, render);
    }

    //Sets base stats, components, and initial display
    protected virtual void Start()
    {
        ec = new EffectCalculator(this, bc);
        bc = new BattleCalculator(this, ec);

        bc.ResetStats();
        UpdateDisplay();

        normal = render.color; //Set the normal color to the sprite's starting color
    }

    /// <summary>
    /// Manages speed bars
    /// </summary>
    public virtual void UpdateTime() {
        if (status == STATUS.DEAD) return; //Do nothing if dead

        pc.ResetPosition();
        CheckLevel();

        if (moveTimer < 100) moveTimer += Time.deltaTime + (Spd / (25f) * speedMultiplier);
        else Ready = true;
    }

    /***STAT METHODS***/
    /// <summary>
    /// Updates the stats canvas on display in battle
    /// </summary>
    public void UpdateDisplay()
    {
        float hpPercentage = Hp / (float)maxHP;

        hpBar.rectTransform.sizeDelta = new Vector2(barsLength * hpPercentage, barsHeight);
    }

    /// <summary>
    /// Used after Order is carried out to reset the speed bar
    /// </summary>
    public void ResetTimer()
    {
        moveTimer = 0;
        ready = false;
        TechTimer--;
        ec.CycleEffects();

        UpdateDisplay();
    }

    /// <summary>
    /// Used by Party classes to indicate which party the Entity belongs to
    /// </summary>
    /// <param name="belongsTo">The party this entity is a part of</param>
    public void SetParty(Party belongsTo)
    {
        party = belongsTo;
    }

    /***AUTOMATIC***/
    protected void OnMouseOver()
    {
        hovering = true;
        ChangeColor("hover");

        party.SetStatsView(true, GetAllStats());
    }

    protected void OnMouseExit()
    {
        hovering = false;
        ChangeColor("normal");

        party.SetStatsView(false, "");
    }

    /***BATTLE METHODS***/
    /// <summary>
    /// Primary Command; uses physical attack based on ATK stat to harm one other entity
    /// </summary>
    /// <param name="e">Entity to attack - can be friendly</param>
    public void Attack()
    {
        SetDefending(false);
        pc.FlipTowardsTarget(bc.target);

        int totalDamage = 0;

        totalDamage = bc.physicalDmg;
        anim.SetTrigger("ATTACK");

        if (bc.landedCrit) totalDamage = (int)(totalDamage * 2.25); //Crit damage
        if (bc.landedHit)
        {
            bc.target.Hp -= totalDamage; //Hit

            //Gain EXP if target is defeated
            if (bc.target.Hp == 0)
            {
                Exp += bc.target.expGain;
            }
        }

        //Miss Animation
        else Miss();

        ResetTimer();
    }

    /// <summary>
    /// Begin the casting of a spell, tech, or skill
    /// </summary>
    /// <param name="type">The type of special ability being used</param>
    public void Cast(string type)
    {
        SetDefending(false);
        pc.FlipTowardsTarget(bc.target);

        if (type == "MAGIC") Hp -= bc.specialCost;
        else if (type == "TECH") bc.techTimer += bc.specialCost + 1; //Add 1 because 1 turn will immediately be reducted after the turn ends

        anim.SetTrigger("SPECIAL");
        bc.activeSpecial.StartAnimation(this, bc.target, bc.landedHit);
        ResetTimer();
    }

    /// <summary>
    /// A special's effect when it finally hits its target
    /// </summary>
    public void SpecialEffect()
    {
        switch (bc.activeSpecial.type)
        {
            case Special.TYPE.ATTACK: bc.OffensiveSpecial(); break;
            case Special.TYPE.HEAL: bc.HealingSpecial(); break;
            case Special.TYPE.REPAIR: bc.RepairSpecial(); break;
            case Special.TYPE.EFFECT: bc.EffectSpecial(); break;

            default: break;
        }

        party.Normalize();
    }
    
    //Miss Animation
    private void Miss()
    {
        if (pc.IsRightOf(bc.target)) bc.target.pc.SetPosition(2f, 0f);
        else bc.target.pc.SetPosition(-2f, 0f);
    }

    //Defense
    /// <summary>
    /// Doubles the user's defensive stats or returns them to normal
    /// </summary>
    /// <param name="b"></param>
    public void SetDefending(bool b)
    {
        if (b)
        {
            Def *= 2;
            Res *= 2;
            Stb *= 2;

            status = STATUS.DEFENDING;
            anim.SetBool("Defending", true);
            ResetTimer();
        }
        else
        {
            Def /= 2;
            if (Def < baseDef) Def = baseDef;

            Res /= 2;
            if (Res < baseRes) Res = baseRes;

            Stb /= 2;
            if (Stb < baseStb) Stb = baseStb;

            status = STATUS.NORMAL;
            anim.SetBool("Defending", false);
        }
    }

    /***GETTER and SETTER METHODS***/
    /// <summary>
    /// Set and get for the index number of this entity in its party
    /// </summary>
    public int Index
    {
        get
        {
            return index;
        }
        set
        {
            index = value;
        }
    }

    /// <summary>
    /// Set and get for HP value
    /// </summary>
    public int Hp
    {
        get { return _hp; }
        set {
            _hp = value; //change

            if (Hp > maxHP) _hp = maxHP;

            //Death
            else if (Hp <= 0)
            {
                _hp = 0;
                status = STATUS.DEAD;
                moveTimer = 0f;
                ready = false;

                AnimationOff();
                anim.SetBool("Dead", true);
                party.UpdateIndeces();
            }

            UpdateDisplay();
        }
    }

    /// <summary>
    /// Set and get for ATK value
    /// </summary>
    public int Atk
    {
        get { return _atk; }
        set { _atk = value; }
    }

    /// <summary>
    /// Set and get for MAG value
    /// </summary>
    public int Mag
    {
        get { return _mag; }
        set { _mag = value; }
    }

    /// <summary>
    /// Set and get for VLT value
    /// </summary>
    public int Vlt
    {
        get { return _vlt; }
        set { _vlt = value; }
    }

    /// <summary>
    /// Set and get for DEF value
    /// </summary>
    public int Def
    {
        get { return _def; }
        set { _def = value; }
    }

    /// <summary>
    /// Set and get for RES value
    /// </summary>
    public int Res
    {
        get { return _res; }
        set { _res = value; }
    }

    /// <summary>
    /// Set and get for STB value
    /// </summary>
    public int Stb
    {
        get { return _stb; }
        set { _stb = value; }
    }

    /// <summary>
    /// Set and get for SKL value
    /// </summary>
    public int Skl
    {
        get { return _skl; }
        set { _skl = value; }
    }

    /// <summary>
    /// Set and get for LCK value
    /// </summary>
    public int Lck
    {
        get { return _lck; }
        set { _lck = value; }
    }

    /// <summary>
    /// Set and get for SPD value
    /// </summary>
    public int Spd
    {
        get { return _spd; }
        set { _spd = value; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns> hovering - if the mouse is over the entity or not </returns>
    public bool IsHovering() { return hovering; }

    /// <summary>
    /// Set and get for the Ready value determining if the entity can use an action or not
    /// </summary>
    public bool Ready
    {
        get { return ready; }
        set {
            ec.StatusEffectsTurn();
            ec.TileEffectsTurn(pc.tile.effect1);
            ec.TileEffectsTurn(pc.tile.effect2);
            ready = value;
        }
    }

    /// <summary>
    /// Getter and setter for STATUS enum
    /// </summary>
    public string GetStatus()
    {
        return status + "";
    }

    /// <summary>
    /// A method which lists all the entity's current stats
    /// </summary>
    /// <returns>A long string with many line breaks to display current statistics</returns>
    public string GetAllStats()
    {
        return Name + "\nLv. " + level + ", XP: " + Exp + "\n" +
            "HP: " + Hp + "\n" +
            type + "\n\n" +
            "ATK: " + Atk + "\n" +
            "MAG: " + Mag + "\n" +
            "VLT: " + Vlt + "\n\n" +
            "DEF: " + Def + "\n" +
            "RES: " + Res + "\n" +
            "STB: " + Stb + "\n\n" +
            "SKL: " + Skl + "\n" +
            "LCK: " + Lck + "\n" +
            "SPD: " + Spd + "\n" +
            "\n" +
            ec.GetAllEffects();
    }

    /// <summary>
    /// Change the entity's color overlay
    /// </summary>
    public void ChangeColor(string code)
    {
        Color colorChange;

        switch (code)
        {
            case "target": colorChange = targeted; break;
            case "active": colorChange = active; break;
            case "normal": colorChange = normal; break;
            case "hover": colorChange = hover; break;
            default: Debug.Log("Not a valid color code: " + code); return;
        }

        render.color = colorChange;

    }

    /// <summary>
    /// The number of turns remaining until a Tech ability can be used again
    /// </summary>
    public int TechTimer
    {
        get
        {
            return bc.techTimer;
        }
        set
        {
            bc.techTimer = value;

            if (bc.techTimer < 0) bc.techTimer = 0;
        }
    }

    /// <summary>
    /// Set the spell the user may use next
    /// </summary>
    /// <param name="spell">A spell to be cast</param>
    public void SetSpecial(int index, string type)
    {
        switch (type)
        {
            case "SKILL": bc.activeSpecial = skills[index]; break;
            case "MAGIC": bc.activeSpecial = spells[index]; break;
            case "TECH": bc.activeSpecial = techs[index]; break;

            case "NULL": bc.activeSpecial = null; break;

            default: break;
        }
    }

    /// <summary>
    /// Returns the special ability the entity is currently using
    /// </summary>
    /// <returns>The active special ability</returns>
    public Special GetSpecial()
    {
        return bc.activeSpecial;
    }

    /// <summary>
    /// Return the game to state NORMAL
    /// </summary>
    public void Normalize()
    {
        party.Normalize();
    }

    //Type Methods

    /// <summary>
    /// Determines if the entity is any of the Organic types
    /// </summary>
    /// <returns>True - if entity type is Organic, Droid/Organic, or Magic/Organic; False - otherwise</returns>
    public bool IsOrganic()
    {
        return
            type == TYPE.ORGANIC ||
            type == TYPE.DROID_ORGANIC ||
            type == TYPE.ORGANIC_MAGIC;
    }

    /// <summary>
    /// Determines if the entity is of any of the Magic types
    /// </summary>
    /// <returns>True - if entity type is Magic, Magic/Droid, or Magic/Organic; False - otherwise</returns>
    public bool IsMagic()
    {
        return
            type == TYPE.MAGIC ||
            type == TYPE.MAGIC_DROID ||
            type == TYPE.ORGANIC_MAGIC;
    }

    /// <summary>
    /// Determines if the entity is of any of the Droid types
    /// </summary>
    /// <returns>True - if entity type is Droid, Droid/Magic, or Droid/Organicc; False - otherwise</returns>
    public bool IsDroid()
    {
        return
            type == TYPE.DROID ||
            type == TYPE.DROID_ORGANIC ||
            type == TYPE.MAGIC_DROID;
    }

    //Animation State

    /// <summary>
    /// Resets the entity's animation state and disables certain triggers
    /// </summary>
    public void AnimationOff()
    {
        anim.SetBool("Dead", false);
        anim.SetBool("Defending", false);
    }

    /***LEVELING UP***/

    /// <summary>
    /// The number of experience points this entity has towards the next level.
    /// Maxes out at 100 regardless of level.
    /// </summary>
    public int Exp
    {
        get { return _exp; }
        set
        {
            _exp = value;
        }
    }

    protected void CheckLevel()
    {

        if (Exp > 100)
        {

            Exp -= 100;

            //Call party
            party.SetLevelUpText(LevelUp(), this);
        }
    }

    protected bool[] LevelUp()
    {
        bool[] ret = new bool[10];

        level++;
        int chance = Random.Range(0, 100);

        //HP
        if (chance < l_hp)
        {
            ret[0] = true;
            maxHP += 1;
            Hp += 1;
        }

        //OFFENSE
        chance = Random.Range(0, 100);
        if (chance < l_atk)
        {
            ret[1] = true;
            baseAtk += 1;
            Atk += 1;
        }
        chance = Random.Range(0, 100);
        if (chance < l_mag)
        {
            ret[2] = true;
            baseMag += 1;
            Mag += 1;
        }
        chance = Random.Range(0, 100);
        if (chance < l_vlt)
        {
            ret[3] = true;
            baseVlt += 1;
            Vlt += 1;
        }

        //DEFENSE
        chance = Random.Range(0, 100); 
        if (chance < l_def)
        {
            ret[4] = true;
            baseDef += 1;
            Def += 1;
        }
        chance = Random.Range(0, 100); 
        if (chance < l_res)
        {
            ret[5] = true;
            baseRes += 1;
            Res += 1;
        }
        chance = Random.Range(0, 100);
        if (chance < l_stb)
        {
            ret[6] = true;
            baseStb += 1;
            Stb += 1;
        }

        //PERFORMANCE
        chance = Random.Range(0, 100);
        if (chance < l_skl)
        {
            ret[7] = true;
            baseSkl += 1;
            Skl += 1;
        }
        chance = Random.Range(0, 100);
        if (chance < l_spd)
        {
            ret[8] = true;
            baseSpd += 1;
            Spd += 1;
        }
        chance = Random.Range(0, 100);
        if (chance < l_lck)
        {
            ret[9] = true;
            baseLck += 1;
            Lck += 1;
        }

        return ret;
    }

    /***MISCELLANEOUS***/

    /// <summary>
    /// Get the party class this entity belongs to
    /// </summary>
    /// <returns>This entity's party</returns>
    public Party GetParty()
    {
        return party;
    }

    /// <summary>
    /// Get the sprite renderer of this entity
    /// </summary>
    /// <returns>An active renderer</returns>
    public SpriteRenderer GetRender()
    {
        return render;
    }

}
