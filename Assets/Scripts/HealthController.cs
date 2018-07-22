using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Class handles interfacing with the Unity engine. Uses associated classed to track and modify data as it pertains to the user health based 
/// parameters provided by other classes when making calls to public methods in this class. Acts as encapsulator for health functionality.
/// </summary>
public class HealthController : MonoBehaviour {

    public HealthSegment[] segmentArray;                // Array of Structs holding health segment data

    private DamageControl   damageControl;              // Class holding all actions taken related to damage
    private HealingControl  healingControl;             // Class holding all actions taken related to healing
    private RechargeControl rechargeControl;            // Class holding all actions taken related to recharge

    public bool universalRecharge;                      // Inspector flag for allowing recharging of segments one at a time
    public bool universalDamageReset;                   // Inspector flag for having any damage reset all segment recharge timers

    public int arraySize;                               // Serialized save data for the custom inspector to track how many elements(segment) exist in the healthSegmentArray

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called at start of scene before the first frame and whenever the object is re-enabled
    private void Awake()
    {
        //Iterate over all the segment for setting up any initial data
        for (int i = 0; i < segmentArray.Length; i++)
        {
            //Check if the segment will be starting with full or no health
            if (segmentArray[i].startActive) { segmentArray[i].currentHealth = segmentArray[i].maxHealth; }
            else { segmentArray[i].currentHealth = 0f; }

            //Make sure recharge timers start with the correct delay
            segmentArray[i].rechargeTimer = segmentArray[i].rechargeDelay;
        }

        //Initialize segment control classes      
        healingControl = new HealingControl(segmentArray, this);
        rechargeControl = new RechargeControl(segmentArray, this, healingControl, universalRecharge, universalDamageReset);
        damageControl = new DamageControl(segmentArray, rechargeControl, this);

        getAllHealthValues();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called every frame while the class/object is enabled
    private void Update()
    {
        rechargeControl.updateCalls();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called for when the object dies - runs out of health on lowest layer 
    private void deathEvent()
    {
        Debug.Log(gameObject.name + " has died!");                          //DEV NOTE - Add your death event here
        gameObject.SetActive(false);
    }

    #region Apply Damage
    /// <summary>
    /// Standard method for applying damage to a HealthController. Method will take provided damage paramter and apply it
    /// to the top level of health (highest array index) and carry to following segments if carryDamageToNextSegment is enabled.
    /// </summary>
    /// <param name="damage">Float value - Damage to be applied</param>
    public void applyDamage(float damage)
    {
        //Iterate though all segment starting at the top layer
        for (int i = segmentArray.Length - 1; i >= 0; i--)
        {
            //Stop applying damage if zero
            if (damage <= 0) return;

            //Only modify non-disbaled segments
            if (segmentArray[i].isDisabled == false)
            {
                //Update damage for segment type
                damage = damageControl.getSegmentModifiedDamage(damage, segmentArray[i]);
                //Apply the damage and get new value
                damage = damageControl.applyDamageToSegment(damage, segmentArray[i]);
                //Notify recharge states that damage has been taken
                rechargeControl.damageTaken(segmentArray[i]);
            }
        }
    }

    /// <summary>
    /// Method for applying damage to a HealthController. Method will take provided damage paramter and a boolean for indicating if 
    /// modifiers will be used on the segment taking damage. Damage will be applied from the top level (highest array index) and will
    /// carry to the following segments if carryDamageToNextSegment is enabled.
    /// </summary>
    /// <param name="damage">Float value - Damage to be applied</param>
    /// <param name="ignoreModifiers">Boolean value - Determines if segment modifiers will be applied to damage paramter</param>
    public void applyDamage (float damage, bool ignoreModifiers)
    {
        for (int i = segmentArray.Length - 1; i >= 0; i--)
        {
            //Stop applying damage if zero
            if (damage <= 0) return;

            //Only modify non-disabled segments
            if (segmentArray[i].isDisabled == false)
            {
                //Update damage for segment type if modifiers are not ignored
                if (ignoreModifiers == false) damage = damageControl.getSegmentModifiedDamage(damage, segmentArray[i]);
                //Apply the damage and get new value
                damage = damageControl.applyDamageToSegment(damage, segmentArray[i]);
                //Notify recharge states that damage has been taken
                rechargeControl.damageTaken(segmentArray[i]);
            }
        }
    }

    /// <summary>
    ///Method for applying damage to a HealthController.Method will take provided damage value, boolean for ingoring segment type
    /// modifiers, and a SegmentType to apply damage to the segments. Damage will only be applied to a segment if the SegmentType matches
    /// between the segment in the array and the paramter. Damage is applied from the top level (highest array index) and will carry
    /// to the following segments that match if carryDamageToNextSegment is enabled.
    /// </summary>
    /// <param name="damage">Float value - Damage to be applied</param>
    /// <param name="ignoreModifiers">Boolean value - Determines if segment modifiers will be applied to damage paramter</param>
    /// <param name="type">SegmentType ENUM to compare with the SegmentType found on each segment</param>
    public void applyDamage(float damage, bool ignoreModifiers, SegmentType type)
    {
        //Code works the same as the above method, this one redirects there to save on space.
        //Only difference is the check for matching top segments. Wanted to provide both for developer ease.
        applyDamage(damage, ignoreModifiers, type, false);
    }

    /// <summary>
    /// Method for applying damage to a HealthController. Method will take provided damage value, boolean for ingoring segment type 
    /// modifiers, and a SegmentType to apply damage to the segments. Damage will only be applied to a segment if the SegmentType matches
    /// between the segment in the array and the paramter. Additionally the onlyRunIfTopSegmentMatches boolean allows for having damage 
    /// be applied only when the top segment matches the SegmentType paramter. 
    /// Damage is applied from the top level (highest array index) and will carry
    /// to the following segments that match if carryDamageToNextSegment is enabled.
    /// </summary>
    /// <param name="damage">Float value - Damage to be applied</param>
    /// <param name="ignoreModifiers">Boolean value - Determines if segment modifiers will be applied to damage paramter</param>
    /// <param name="type">SegmentType ENUM to compare with the SegmentType found on each segment</param>
    /// <param name="onlyRunIfTopSegmentMatches">Boolean value - Damage will only be applied if the top segment matches the SegmentType paramter</param>
    public void applyDamage (float damage, bool ignoreModifiers, SegmentType type, bool onlyRunIfTopSegmentMatches)
    {
        //Check for top health segment matching the SegmentType param
        if (onlyRunIfTopSegmentMatches)
        {
            //Iterate though the segmentArray looking for the top segment with health
            for (int i = segmentArray.Length - 1; i >= 0; i--)
            {
                if (segmentArray[i].currentHealth > 0 && segmentArray[i].isDisabled == false)
                {
                    //If the top segment found does not match we do nothing - return void
                    if (segmentArray[i].segmentType != type) return;
                }
            }
        }

        //Iterate through the segment array 
        for (int i = segmentArray.Length - 1; i >= 0; i--)
        {
            //Stop applying damage if zero
            if (damage <= 0) return;

            //If the segment is not disabled and matches the type, we apply damage
            if (segmentArray[i].isDisabled == false && segmentArray[i].segmentType == type)
            {
                //Modify damage if ignoreModifiers is false
                if (ignoreModifiers == false) damage = damageControl.getSegmentModifiedDamage(damage, segmentArray[i]);
                //Apply damage to the segment and get remaining damage
                damage = damageControl.applyDamageToSegment(damage, segmentArray[i]);
                //Notify recharge states that damage has been taken
                rechargeControl.damageTaken(segmentArray[i]);
            }
        }
    }

    /// <summary>
    /// Method for applying damage to a HealthController. Method will take provided damage value, boolean for ignoring segment type
    /// modifiers, and a string tag. Damage will only be applied to a segment if the provided tag matches one of the tags on a segment.
    /// Damage is applied from the top level (highest array index) and will carry to the following segments that match if 
    /// carryDamageToNextSegment is enabled. 
    /// </summary>
    /// <param name="damage">Float value - Damage to be applied</param>
    /// <param name="ignoreModifiers">Boolean value - Determines if segment modifiers will be applied to damage paramter</param>
    /// <param name="tag">String value - Checked against strings in the specialTags array on a segment</param>
    public void applyDamage (float damage, bool ignoreModifiers, string tag)
    {
        //Iterate though all segments in the array
        for (int i = segmentArray.Length - 1; i >= 0; i--)
        {
            //If we have no damage, return
            if (damage <= 0) return;

            //If the segment specialTag matches and is not disabled we apply damage
            if (stringMatched(new string[] { tag }, segmentArray[i]) && segmentArray[i].isDisabled == false)
            {
                //Modify damage if ingoreModifiers is false
                if (ignoreModifiers == false) damage = damageControl.getSegmentModifiedDamage(damage, segmentArray[i]);
                //Apply damage to the segment and get remaining damage
                damage = damageControl.applyDamageToSegment(damage, segmentArray[i]);
                //Notify recharge states that damage has been taken
                rechargeControl.damageTaken(segmentArray[i]);
            }
        }
    }

    /// <summary>
    /// Method for applying damage to a HealthController. Method will take provided damage value, boolean for ignoring segment type
    /// modifiers, and a string array. Damage will only be applied to a segment if the provided array matches one of the tags on a segment.
    /// Damage is applied from the top level (highest array index) and will carry to the following segments that match if 
    /// carryDamageToNextSegment is enabled. 
    /// </summary>
    /// <param name="damage">Float value - Damage to be applied</param>
    /// <param name="ignoreModifiers">Boolean value - Determines if segment modifiers will be applied to damage paramter</param>
    /// <param name="tags">String array - Checked against strings in the specialTags array on a segment</param>
    public void applyDamage (float damage, bool ignoreModifiers, string[] tags)
    {
        //Iterate though all segments in the array
        for (int i = segmentArray.Length - 1; i >= 0; i--)
        {
            //If we have no damage, return
            if (damage <= 0) return;

            //If the segment specialTag matches and is not disabled we apply damage
            if (stringMatched(tags, segmentArray[i]) && segmentArray[i].isDisabled == false)
            {
                //Modify damage if ingoreModifiers is false
                if (ignoreModifiers == false) damage = damageControl.getSegmentModifiedDamage(damage, segmentArray[i]);
                //Apply damage to the segment and get remaining damage
                damage = damageControl.applyDamageToSegment(damage, segmentArray[i]);
                //Notify recharge states that damage has been taken
                rechargeControl.damageTaken(segmentArray[i]);
            }
        }
    }

    /// <summary>
    /// Method for applying damage directly to a specified segment without checking any other segments. IgnoreModifiers 
    /// boolean included to allow for skipping damage to a segment if enabled, otherwise damage is applied normally with
    /// no carry over to following segments. 
    /// Providing an invalid index for the array will result in no action being taken.
    /// </summary>
    /// <param name="damage">Float value - Damage to be applied</param>
    /// <param name="ignoreModifiers">Boolean value - Determines if the segment modifiers will be applied to the damage parameter</param>
    /// <param name="index">Index of the HealthSegment in the segmentArray</param>
    public void applyDamage (float damage, bool ignoreModifiers, int index)
    {
        //Do nothing if we have a bad index
        if (index < 0 || index >= segmentArray.Length) return;

        //Modify the damage if enabled
        if (ignoreModifiers == false) damage = damageControl.getSegmentModifiedDamage(damage, segmentArray[index]);
        //Apply the damage with no catch on the return
        damageControl.applyDamageToSegment(damage, segmentArray[index]);

        //Notify recharge states that damage was taken
        rechargeControl.damageTaken(segmentArray[index]);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Takes two arrays of strings (parmeter and segment specialTags) and compares the elements of each, returning true 
    //if any matches are found and false if no strings match in the arrays
    private bool stringMatched (string[] newTags, HealthSegment segment)
    {
        //Iterate through both string arrays to check each comparison
        foreach (string tag in segment.specialTags)
        {
            foreach (string newTag in newTags)
            {
                //If we find a match we return true
                if (tag.Equals(newTag)) return true;
            }
        }

        //No match was found, return false
        return false;
    }
    #endregion

    #region Apply Health
    /// <summary>
    /// Simple call for applying health to the segment array. Method will start at index zero and add to currentHealth. 
    /// If the carryHealingToNextSegment boolean is enabled the method will continue to the next index and apply health there.
    /// Process will repeat until health reaches zero or no carry over has been specified on the segment. Providing a health 
    /// parameter of zero or less will result in no action taken.
    /// </summary>
    /// <param name="health">Float value - Amount of health to be applied</param>
    public void applyHealth (float health)
    {
        //Iterate through the segment array
        for (int i = 0; i < segmentArray.Length; i++)
        {
            //Stop applying health if there is none left
            if (health <= 0) return;

            //Apply health and get remaining for next segment - will return zero if carry healing disabled
            health = healingControl.applyHealthToSegment(health, segmentArray[i]);
        }
    }

    /// <summary>
    /// Called to apply health based on segmentType from each segment. Method starts at index zero and adds health to
    /// currentHealth. If the carryHealingToNextSegment boolean is enabled the method will continue to the next index and apply health there.
    /// Process will repeat until health reaches zero or no carry over has been specified on the segment. Providing a health 
    /// parameter of zero or less will result in no action taken.
    /// </summary>
    /// <param name="health">Float value - Amount of health to be applied</param>
    /// <param name="type">SegmentType ENUM - Type of segment to apply health to</param>
    public void applyHealth (float health, SegmentType type)
    {
        //Iterate through the segment array
        for (int i = 0; i < segmentArray.Length; i++)
        {
            //Stop applying health if there is none left
            if (health <= 0) return;

            //Only apply health if segment types match
            if (segmentArray[i].segmentType == type)
            {
                health = healingControl.applyHealthToSegment(health, segmentArray[i]);
            }
        }
    }

    /// <summary>
    /// Called to apply health if the provided string (tag) matches one of the tags found in the specialTags array
    /// for the current segment. Method starts at index zero and adds health to
    /// currentHealth. If the carryHealingToNextSegment boolean is enabled the method will continue to the next index and apply health there.
    /// Process will repeat until health reaches zero or no carry over has been specified on the segment. Providing a health 
    /// parameter of zero or less will result in no action taken.
    /// </summary>
    /// <param name="health">Float value - Amount of health to be applied</param>
    /// <param name="tag">String value - string to match to one of the strings in the specialTags array for each segment</param>
    public void applyHealth (float health, string tag)
    {
        //Iterate through the segment array
        for (int i = 0; i < segmentArray.Length; i++)
        {
            //Stop applying health if there is none left
            if (health <= 0) return;

            //Only apply health if tag matches one of the special tags
            if (stringMatched(new string[] {tag}, segmentArray[i]))
            {
                health = healingControl.applyHealthToSegment(health, segmentArray[i]);
            }
        }
    }

    /// <summary>
    /// Called to apply health if one of the tags in the provied string array matches one of the strings in the segment's
    /// specialTags array. Method starts at index zero and adds health to
    /// currentHealth. If the carryHealingToNextSegment boolean is enabled the method will continue to the next index and apply health there.
    /// Process will repeat until health reaches zero or no carry over has been specified on the segment. Providing a health 
    /// parameter of zero or less will result in no action taken.
    /// </summary>
    /// <param name="health">Float value - Amount of health to be applied</param>
    /// <param name="tags">Array of strings to compare with the specialTags array on the segment</param>
    public void applyHealth (float health, string[] tags)
    {
        //Iterate through the segment array
        for (int i = 0; i < segmentArray.Length; i++)
        {
            //Stop applying health if there is none left
            if (health <= 0) return;

            //Only apply health if one of the provided tags matches the specialTags array
            if (stringMatched(tags, segmentArray[i]))
            {
                health = healingControl.applyHealthToSegment(health, segmentArray[i]);
            }
        }
    }

    /// <summary>
    /// Called to apply health directly to a segment based on a provided index for the segmentArray. 
    /// Providing an index out of the array bounds will result in no action taken. Providing a health value of zero or
    /// less will result in no action taken.
    /// </summary>
    /// <param name="health">Float value - Amount of health to be applied</param>
    /// <param name="index">Int value - index location of the segment to apply health to</param>
    public void applyHealth (float health, int index)
    {
        //Prevent silly calls from running
        if (health <= 0) return;
        if (index < 0 || index >= segmentArray.Length) return;

        //Add health and fix any overflow
        segmentArray[index].currentHealth += health;
        if (segmentArray[index].currentHealth > segmentArray[index].maxHealth)
            segmentArray[index].currentHealth = segmentArray[index].maxHealth;
    }
    #endregion

    #region Getter Methods
    /// <summary>
    /// Getter method that returns an array of floats representing the currentHealth variable on each of the HealthSegments
    /// found in the segmentArray array.
    /// </summary>
    /// <returns>Array of floats holding all currentHealth segment values</returns>
    public float[] getAllHealthValues ()
    {
        float[] temp = new float[segmentArray.Length];

        for (int i = 0; i < segmentArray.Length; i++)
        {
            temp[i] = segmentArray[i].currentHealth;
        }

        return temp;
    }

    /// <summary>
    /// Getter method for finding the number of segments (or array length) of the segmentArray. 
    /// You can also consider this to be the number of health segments this class has. 
    /// </summary>
    /// <returns></returns>
    public int getNumberOfSegments ()
    {
        return segmentArray.Length;
    }

    /// <summary>
    /// Getter method for retrieving a single HealthSegment from the segmentArray array. Use this if you would like 
    /// to manually read or edit variables inside each segment instead of relying on the provided public methods for 
    /// data manipulation.
    /// 
    /// Returns null if an invalid index is provided. Use getNumberOfSegments() call to make sure you do not try to 
    /// access an element that does not exist.
    /// </summary>
    /// <param name="segmentNumber">Index location of the HealthSegment in the array</param>
    /// <returns>HealthSegment struct at the provided index</returns>
    public HealthSegment? getHealthSegment (int segmentNumber)
    {
        if (segmentNumber >= segmentArray.Length || segmentNumber < 0) return null;

        return segmentArray[segmentNumber];
    }

    /// <summary>
    /// Getter method for retreiving the current segment that damage will be applied to if any applyDamage methods are 
    /// called. 
    /// 
    /// Returns null if all segments have no health or are disabled. The unit should be dead by that point so I'm not
    /// sure why you're calling this method. 
    /// </summary>
    /// <returns>First HealthSegment that damage can be applied to</returns>
    public HealthSegment? getCurrentHealthSegment ()
    {
        //Run through all the segments from the top down
        for (int i = segmentArray.Length - 1; i >= 0; i--)
        {
            //If we find one with health that's not disabled we return it
            if (segmentArray[i].currentHealth > 0 && segmentArray[i].isDisabled == false) return segmentArray[i];
        }

        //Failsafe call - should never happen
        return null;
    }

    /// <summary>
    /// Getter method for retrieving the entire array of HealthSegments. 
    /// </summary>
    /// <returns>Array of HealthSegment</returns>
    public HealthSegment[] getSegmentArray ()
    {
        return segmentArray;
    }
    #endregion   
}

#region Segment Data
/// <summary>
/// Struct used for holding all relevant data on a health segment. Structs used for access and modification speeds to cope with the likeihood that
/// developers will be using this across a large number of objects and/or making modifications frequently. (Damage over time effects for example)
/// </summary>
[System.Serializable]
public struct HealthSegment
{
    public float maxHealth;
    public bool startActive;   

    public bool carryDamageToNextSegment;
    public bool carryHealingToNextSegment;
    public bool isDisabled;

    public SegmentType segmentType;
    public bool useTags;
    public string[] specialTags;

    public bool canRecharge;
    public bool damageResetsRecharge;
    public float rechargeRate;  
    public float rechargeDelay;

    public float armourDamageReduction; 
    public float minimumArmourDamage;   

    public float constantShieldDamage; 

    public float barrierDamageMitigation;  


    public float currentHealth;   
    public float rechargeTimer;  
}

/// <summary>
/// Enum used for tracking what type of segment the HealthSegment is. Housed inside the HealthSegment Struct and for checks against the state of the 
/// enum inside the struct
/// </summary>
public enum SegmentType { health, armour, shield, barrier }
#endregion

#region Control Classes
/// <summary>
/// Class handles all modification of segment values whenever an event occurs where damage has been taken.
/// </summary>
class DamageControl
{
    private HealthSegment[] segmentArray;
    private HealthController parent;
    private RechargeControl rechargeControl;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Constructor
    public DamageControl(HealthSegment[] segmentArray, RechargeControl rechargeControl, HealthController parent)
    {
        this.segmentArray = segmentArray;
        this.rechargeControl = rechargeControl;
        this.parent = parent;
    }

    #region Damage Modification
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Returns a modified float of the provided "damage" param based on the SegmentType of the provided segment
    internal float getSegmentModifiedDamage (float damage, HealthSegment segment)
    {
        //Return modified damage based on segment type
        switch (segment.segmentType)
        {
            case SegmentType.health:
                return damage;
            case SegmentType.armour:
                return modifyArmourDamage(damage, segment.armourDamageReduction, segment.minimumArmourDamage);
            case SegmentType.shield:
                return modifyShieldDamage(segment.constantShieldDamage);
            case SegmentType.barrier:
                return modifyBarrierDamage(damage, segment.barrierDamageMitigation);
        }

        //Base return case that should never be reached
        Debug.LogError("HEALTH CONTROLLER SCRIPT - Failed SegmentType Comparison - Segment type could not be determined when modying damage to be taken. GameObject: " + parent.gameObject.name);
        return damage;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Returns damage value modified by the reduction applied on an armour segment. If reduced damage falls below the 
    //provided minimum, the minimum is returned
    private float modifyArmourDamage (float damage, float reduction, float min)
    {
        if (damage - reduction < min)
        {
            return min;
        }
        else
        {
            return (damage - reduction);
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Currently shield always applies the constant amount of damage making this method do virtually nothing, however should
    //that be changed in the future I have included the method for formatting
    private float modifyShieldDamage (float constantDamage)
    {
        return constantDamage;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Returns damage as a mutliple of the factor parameter. Factor is expected to be less than zero to act as a percentage
    private float modifyBarrierDamage (float damage, float factor)
    {
        return (damage * factor);
    }
    #endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Handles applying damage to a specified segment based on the param values. Returns the remaining damage if there is 
    //extra damage to be applied to the next segment if the current segment has carryDamageToNextSegment set true.
    internal float applyDamageToSegment (float damage, HealthSegment segment)
    {
        //If we have more damage to apply than the segment has health
        if (segment.currentHealth < damage)
        {
            //Update damage with the difference and set current health to zero
            damage -= segment.currentHealth;
            segment.currentHealth = 0;
        }
        //Otherwise we can just apply all damage
        else
        {
            segment.currentHealth -= damage;
            damage = 0f;
        }

        //Return the remaining damage if we are carrying to the next segment
        if (segment.carryDamageToNextSegment) return damage;
        //Otherwise we will have no damage to continue with
        else return 0f;
    }
}

/// <summary>
/// Class handles all modification of values whenever an event occurs where healing has been applied
/// </summary>
class HealingControl
{
    private HealthSegment[] segmentArray;
    private HealthController parent;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Constructor
    public HealingControl(HealthSegment[] segArray, HealthController parent)
    {
        segmentArray = segArray;
        this.parent = parent;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Handles applying health to the specified segment provided on the paramters. Returns remaining health if healing is 
    //carried to the next segment, else returns zero
    internal float applyHealthToSegment (float health, HealthSegment segment)
    {
        //If we have more health than possible to add
        if (segment.maxHealth - segment.currentHealth < health)
        {
            //Update health for remaining value and set current health to max
            health -= segment.maxHealth - segment.currentHealth;
            segment.currentHealth = segment.maxHealth;
        }
        //Otherwise we can just apply all the health
        else
        {
            //Add health and set health to zero
            segment.currentHealth += health;
            health = 0f;
        }

        if (segment.carryHealingToNextSegment) return health;
        else return 0f;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called to add a set amount of health a specified segment
    internal void addHealth (float health, HealthSegment segment)
    {
        segment.currentHealth += health;
        if (segment.currentHealth > segment.maxHealth) segment.currentHealth = segment.maxHealth;
    }
}

/// <summary>
/// Class handles keeping track of recharge times and modification for all segments during the course of gameplay
/// </summary>
class RechargeControl
{
    private bool universalRecharge;
    private bool universalReset;

    private HealthSegment[] segmentArray;
    private HealthController parent;
    private HealingControl healingControl;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Constructor
    public RechargeControl(HealthSegment[] segArray, HealthController parent, HealingControl healingControl, bool universalRecharge, bool universalReset)
    {
        segmentArray = segArray;
        this.parent = parent;
        this.universalRecharge = universalRecharge;
        this.universalReset = universalReset;
        this.healingControl = healingControl;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called from the Update method for any methods that need to be called every frame in this class
    internal void updateCalls ()
    {
        updateRechargeTimers();
        updateRechargeHealth();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called whenever damage is taken by a segment to update the recharge states of either the damaged segment or all segments
    internal void damageTaken (HealthSegment segment)
    {
        //Reset all the segment recharges
        if (universalReset)
        {
            //Iterate through all segments
            for (int i = 0; i < segmentArray.Length; i++)
            {
                //Only reset timer if damage reset is enabled
                if (segmentArray[i].damageResetsRecharge)
                {
                    segmentArray[i].rechargeTimer = segmentArray[i].rechargeDelay;
                }
            }
        }
        //Otherwise we only reset this segment
        else
        {
            //Only reset timer if damage reset enabled
            if (segment.damageResetsRecharge)
            {
                segment.rechargeTimer = segment.rechargeDelay;
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called every frame to update recharge timers on each segment
    private void updateRechargeTimers ()
    {
        for (int i = 0; i < segmentArray.Length; i++)
        {
            if (segmentArray[i].canRecharge && segmentArray[i].rechargeTimer > 0 && segmentArray[i].isDisabled == false)
            {
                segmentArray[i].rechargeTimer -= Time.deltaTime;
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called to update the health of each segment when needed after the recharge timer has expired
    private void updateRechargeHealth ()
    {
        //Recharge only one segment at a time
        if (universalRecharge)
        {
            //Iterate through possible segments from the lowest index
            for (int i = 0; i < segmentArray.Length; i++)
            {
                //If the segment is missing health and the timer has expired
                if (segmentArray[i].currentHealth < segmentArray[i].maxHealth && segmentArray[i].rechargeTimer <= 0 && segmentArray[i].isDisabled == false)
                {
                    healingControl.addHealth(Time.deltaTime * segmentArray[i].rechargeRate, segmentArray[i]);
                    return; //Prevents later segments from recharging this frame
                }
            }
        }
        //Otherwise we can recharge all segments at once
        else
        {
            //Iterate through segments
            for (int i = 0; i < segmentArray.Length; i++)
            {
                //If the segment is missing health and the recharge timer has expired
                if (segmentArray[i].currentHealth < segmentArray[i].maxHealth && segmentArray[i].rechargeTimer <= 0 && segmentArray[i].isDisabled == false)
                {
                    healingControl.addHealth(Time.deltaTime * segmentArray[i].rechargeRate, segmentArray[i]);
                }
            }
        }
    }
}
#endregion