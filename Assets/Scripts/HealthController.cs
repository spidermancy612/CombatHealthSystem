using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Class handles interfacing with the Unity engine. Uses associated classed to track and modify data as it pertains to the user health based 
/// parameters provided by other classes when making calls to public methods in this class. Acts as encapsulator for health functionality.
/// </summary>
public class HealthController : MonoBehaviour {

    public HealthSegement[] segmentArray;         // Array of Structs holding health segment data

    private DamageControl   damageControl;
    private HealingControl  healingControl;
    private RechargeControl rechargeControl;



    private int currentSegment;                         // Current segment for applying damage
    private int currentRechargeSegment;                 // Current segment for updating the recharge state

    private bool canRechargeSomeSegment;                // Boolean set to false if no segments have recharge options - saves on segment checks for performance

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
        rechargeControl = new RechargeControl(segmentArray, this);
        damageControl = new DamageControl(segmentArray, rechargeControl, this);

        //float test = damageControl.applyDamageToSegment(10, 1);
    }






    #region Recharge Health Control
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called every frame while the component is active
    private void Update()
    {
        //Update which segment should be tracked for recharging next
        if (canRechargeSomeSegment)
        {
            //Recharge segments in order
            if (universalRecharge)
            {
                updateTopRechargeSegment();
                rechargeTopSegment();
            }
            //Otherwise recharge all segments independently
            else
            {
                updateAllSegmentsForRecharge();
            }
        }

        if (segmentArray[0].currentHealth <= 0) deathEvent();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called on Update to keep track of the current segment of health to take damage
    private void updateTopHealthSegment()
    {
        //Iterates from the highest segment down to apply damage in the correct order
        for (int i = segmentArray.Length - 1; i >= 0; i--)
        {
            if (segmentArray[i].currentHealth > 0)
            {
                currentSegment = i;
                return;
            }
        }

        //All segments have no health - object is dead
        currentSegment = -1;
        deathEvent();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called on Update to keep track of what segment should be recharging next
    private void updateTopRechargeSegment()
    {
        for (int i = 0; i < segmentArray.Length; i++)
        {
            if (segmentArray[i].currentHealth < segmentArray[i].maxHealth && segmentArray[i].canRecharge)
            {
                currentRechargeSegment = i;
                return;
            }
        }
        //Fall off case where there is nothing to recharge
        currentRechargeSegment = -1;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called when the universalRecharge boolean is true to recharge only one segment at a time from the lowest to highest level
    private void rechargeTopSegment ()
    {
        //Edge case where no recharge occurs this frame. We return to save CPU time
        if (canRechargeSomeSegment == false || currentRechargeSegment == -1) return;

        //We can recharge the current segment - mainly for error prevention
        if ((segmentArray[currentRechargeSegment].currentHealth < segmentArray[currentRechargeSegment].maxHealth))
        {
            //We need to decriment the timer
            if (segmentArray[currentRechargeSegment].rechargeTimer > 0)
            {
                segmentArray[currentRechargeSegment].rechargeTimer -= Time.deltaTime;
            }
            //Otherwise timer expired and we recharge health
            else
            {
                segmentArray[currentRechargeSegment].currentHealth += Time.deltaTime * segmentArray[currentRechargeSegment].rechargeRate;
            }

            //Make sure current health does not exceed max health
            if (segmentArray[currentRechargeSegment].currentHealth > segmentArray[currentRechargeSegment].maxHealth)
            {
                segmentArray[currentRechargeSegment].currentHealth = segmentArray[currentRechargeSegment].maxHealth;
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called when the universalRecharge boolean is false to recharge all segments independently at the same time
    private void updateAllSegmentsForRecharge ()
    {
        //Update for all segments 
        for (int i = 0; i < segmentArray.Length; i++)
        {
            //If the current segment can recharge and is missing health
            if (segmentArray[i].canRecharge && segmentArray[i].currentHealth < segmentArray[i].maxHealth)
            {
                //Check for timer still being active
                if (segmentArray[i].rechargeTimer > 0)
                {
                    segmentArray[i].rechargeTimer -= Time.deltaTime;
                }
                //Otherwise we can increment health
                else
                {
                    segmentArray[i].currentHealth += segmentArray[i].rechargeRate * Time.deltaTime;
                    //Make sure health doesn't overflow max specified amount
                    if (segmentArray[i].currentHealth > segmentArray[i].maxHealth) segmentArray[i].currentHealth = segmentArray[i].maxHealth;
                }
            }
        }
    }
    #endregion

    #region Apply Damage
    /// <summary>
    /// Standard method for applying damage to a HealthController. Method will take provided damage paramter and apply it
    /// to the top level of health (highest array index) and carry to following segments if carryDamageToNextSegment is enabled.
    /// </summary>
    /// <param name="damage">Float value of damage to be applied</param>
    public void applyDamage(float damage)
    {
        //Iterate though all segment starting at the top layer
        for (int i = segmentArray.Length - 1; i >= 0; i--)
        {
            //Stop applying damage if zero
            if (damage == 0) return;

            //Only modify non-disbaled segments
            if (segmentArray[i].isDisabled == false)
            {
                //Update damage for segment type
                damage = damageControl.getSegmentModifiedDamage(damage, i);
                //Apply the damage and get new value
                damage = damageControl.applyDamageToSegment(damage, i);
            }
        }
    }

    /// <summary>
    /// Method for applying damage to a HealthController. Method will take provided damage paramter and a boolean for indicating if 
    /// modifiers will be used on the segment taking damage. Damage will be applied from the top level (highest array index) and will
    /// carry to the following segments if carryDamageToNextSegment is enabled.
    /// </summary>
    /// <param name="damage">Float value of damage to be applied</param>
    /// <param name="ignoreModifiers">Boolean value determining if segment modifiers are ignored</param>
    public void applyDamage (float damage, bool ignoreModifiers)
    {
        for (int i = segmentArray.Length - 1; i >= 0; i--)
        {
            //Stop applying damage if zero
            if (damage == 0) return;

            //Only modify non-disabled segments
            if (segmentArray[i].isDisabled == false)
            {
                //Update damage for segment type if modifiers are not ignored
                if (ignoreModifiers == false) damage = damageControl.getSegmentModifiedDamage(damage, i);
                //Apply the damage and get new value
                damage = damageControl.applyDamageToSegment(damage, i);
            }
        }
    }

    /// <summary>
    /// Method for applying damage to a HealthController. Method will take provided damage value, boolean for ingoring segment type 
    /// modifiers, and a SegmentType to apply damage to the segments. Damage will only be applied to a segment if the SegmentType matches
    /// between the segment in the array and the paramter. Additionally the onlyRunIfTopSegmentMatches boolean allows for having damage 
    /// be applied only when the top segment matches the SegmentType paramter. 
    /// Damage is applied from the top level (highest array index) and will carry
    /// to the following segments that match if carryDamageToNextSegment is enabled.
    /// </summary>
    /// <param name="damage">Float value of damage to be applied</param>
    /// <param name="ignoreModifiers">Boolean value determining if segment modifiers are ignored</param>
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
            if (damage == 0) return;

            //If the segment is not disabled and matches the type, we apply damage
            if (segmentArray[i].isDisabled == false && segmentArray[i].segmentType == type)
            {
                //Modify damage if ignoreModifiers is false
                if (ignoreModifiers == false) damage = damageControl.getSegmentModifiedDamage(damage, i);
                //Apply damage to the segment and get remaining damage
                damage = damageControl.applyDamageToSegment(damage, i);
            }
        }
    }

    /// <summary>
    ///Method for applying damage to a HealthController.Method will take provided damage value, boolean for ingoring segment type
    /// modifiers, and a SegmentType to apply damage to the segments. Damage will only be applied to a segment if the SegmentType matches
    /// between the segment in the array and the paramter. Damage is applied from the top level (highest array index) and will carry
    /// to the following segments that match if carryDamageToNextSegment is enabled.
    /// </summary>
    /// <param name="damage">Float value of damage to be applied</param>
    /// <param name="ignoreModifiers">Boolean value determining if segment modifiers are ignored</param>
    /// <param name="type">SegmentType ENUM to compare with the SegmentType found on each segment</param>
    public void applyDamage (float damage, bool ignoreModifiers, SegmentType type)  
    {
        //Code works the same as the above method, this one redirects there to save on space.
        //Only difference is the check for matching top segments. Wanted to provide both for developer ease.
        applyDamage(damage, ignoreModifiers, type, false); 
    }
    #endregion

    #region Apply Bonus Damage
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Applies damage based on paramters compared to segment. If paramters match (SegmentType) then provided bonus damage
    //will be added to the damage applied. If not, only the base damage will be applied.
    //Additionally allows for the specification of ignoring modifers
    public void applyBonusDamage(float baseDamage, float bonusDamage, SegmentType type, bool ignoreModifiers)
    {
        //Make sure we're on the right segment - important call for checking the segment before applying damage
        updateTopHealthSegment();

        //Check if we match with the provided segment type
        if (segmentArray[currentSegment].segmentType == type)
        {
            //Check if we are ignoring the segment damage modifiers
            if (ignoreModifiers) applyDamageToSegment(baseDamage + bonusDamage, true);
            else applyDamage(baseDamage + bonusDamage);
        }
        //Otherwise we aren't applying bonus damage
        else
        {
            //Check if we're ignoring the segment damage modifiers
            if (ignoreModifiers) applyDamageToSegment(baseDamage, true);
            else applyDamage(baseDamage);
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Applies damage based on paramters compared to segment. If paramters match (string) then provided bonus damage
    //will be added to the damage applied. If not, only the base damage will be applied.
    //Additionally allows for the specification of ignoring modifers
    public void applyBonusDamage (float baseDamage, float bonusDamage, string tag, bool ignoreModifiers)
    {
        updateTopHealthSegment();

        //If the segment has no tags or has been set to not use tags we return and avoid doing any checks
        if (segmentArray[currentSegment].useTags == false || segmentArray[currentSegment].specialTags.Length == 0) return;

        //Check each string in the tags array for a match
        foreach (string t in segmentArray[currentRechargeSegment].specialTags)
        {
            //If we find a match then apply the bonus damage and return
            if (t.Equals(tag))
            {
                if (ignoreModifiers) applyDamageToSegment(baseDamage + bonusDamage, true);
                else applyDamage(baseDamage + bonusDamage);
                return;
            }
        }

        //We didn't find the tag in the array, apply only base damage
        if (ignoreModifiers) applyDamageToSegment(baseDamage, true);
        else applyDamage(baseDamage);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Applies damage based on paramters compared to segment. If paramters match (string array) then provided bonus damage
    //will be added to the damage applied. If not, only the base damage will be applied.
    //Additionally allows for the specification of ignoring modifers
    public void applyBonusDamage(float baseDamage, float bonusDamage, string[] tagArray, bool ignoreModifiers)
    {
        updateTopHealthSegment();

        //If the segment has no tags or has been set to not use tags we return and avoid doing any checks
        if (segmentArray[currentSegment].useTags == false || segmentArray[currentSegment].specialTags.Length == 0) return;

        //Check each string in the tags array
        foreach (string st in segmentArray[currentRechargeSegment].specialTags)
        {
            foreach (string t in tagArray)
            {
                if (st.Equals(t))
                {
                    if (ignoreModifiers) applyDamageToSegment(baseDamage + bonusDamage, true);
                    else applyDamage(baseDamage + bonusDamage);
                    return;
                }
            }
        }

        //We didn't find the tag in the array, apply only base damage
        if (ignoreModifiers) applyDamageToSegment(baseDamage, true);
        else applyDamage(baseDamage);
    }
    #endregion

    #region ApplyHealth 
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Default public call for adding health to the first applicable segment and any following segments based on carry settings
    public void applyHealth (float health)
    {
        //iterate through all segments
        for (int i = 0; i < segmentArray.Length; i++)
        {
            //if the current segment is missing health we apply healing
            if (segmentArray[i].currentHealth < segmentArray[i].maxHealth)
            {
                //Record altered health value - method also causing healing to segment
                health = applyHealthToSegment(health, i);

                //Once we have no more health to apply we stop trying to heal segments
                if (health <= 0) return;
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Public call for adding health to the first segment that matches the SegmentType ENUM and carries healing to other
    //matching ENUMS based on carry settings
    public void applyHealth (float health, SegmentType type)
    {
        //iterate through all segments
        for (int i = 0; i < segmentArray.Length; i++)
        {
            //if the current segment is missing health and matches the segment type we appy healing
            if (segmentArray[i].currentHealth < segmentArray[i].maxHealth && segmentArray[i].segmentType == type)
            {
                //Record altered health value - method also causing healing to segment
                health = applyHealthToSegment(health, i);

                //Once we have no more health to apply we stop trying to heal segments
                if (health <= 0) return;
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Public call for adding health to the first segment that matches the provided string tag and carries healing over 
    //to matching segments based on carry settings
    public void applyHealth (float health, string tag)
    {
        //iterate through all segments
        for (int i = 0; i < segmentArray.Length; i++)
        {
            //if the current segment is missing health we apply healing
            if (segmentArray[i].currentHealth < segmentArray[i].maxHealth && tagDoesMatch(new string[]{tag}, i))
            {
                //Record altered health value - method also causing healing to segment
                health = applyHealthToSegment(health, i);

                //Once we have no more health to apply we stop trying to heal segments
                if (health <= 0) return;
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Public call for adding health to the first segment that matches the provided string array of tags and carries healing over 
    //to matching segments based on carry settings
    public void applyHealth(float health, string[] tags)
    {
        //iterate through all segments
        for (int i = 0; i < segmentArray.Length; i++)
        {
            //if the current segment is missing health we apply healing
            if (segmentArray[i].currentHealth < segmentArray[i].maxHealth && tagDoesMatch(tags, i))
            {
                //Record altered health value - method also causing healing to segment
                health = applyHealthToSegment(health, i);

                //Once we have no more health to apply we stop trying to heal segments
                if (health <= 0) return;
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Compares two arrays of strings looking for a match. Returns a boolean if that match status
    private bool tagDoesMatch (string[] tags, int index)
    {
        //Checlk all special tags on the segment
        foreach (string segTag in segmentArray[index].specialTags)
        {
            //Check against all provided tags
            foreach (string tag in tags)
            {
                //return true if we find a match
                if (segTag.Equals(tag)) return true;
            }
        }
        //Failed to find any matches
        return false;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Handles applying the provided health value to the specified segment and returns the difference after adding.
    //Positive return value indicates health overflow and there will need to be health applied to later segments
    private float applyHealthToSegment (float health, int index)
    {
        //Add healing amount
        segmentArray[index].currentHealth += health;
        //Record the difference between current health and max health (positive value indicates overflow)
        health = segmentArray[index].currentHealth - segmentArray[index].maxHealth;
        //If current health has overflown we correct current health to max
        if (health > 0) segmentArray[index].currentHealth = segmentArray[index].maxHealth;

        //If we can carry healing then return the remaining health to apply - or return 0 if no healing is to be carried
        if (segmentArray[index].carryHealingToNextSegment) return health;
        else return 0f;
    }
    #endregion

    #region Getter Methods
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Getter method for providing the current health of all segments
    public float[] getAllHealthValues ()
    {
        float[] temp = new float[segmentArray.Length];

        for (int i = 0; i < segmentArray.Length; i++)
        {
            temp[i] = segmentArray[i].currentHealth;
        }

        return temp;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Getter method for finding the number of segments tracked in this class
    public int getNumberOfSegments ()
    {
        return segmentArray.Length;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Returns the HealthSegment struct at the specified index within the healthSegmentArray
    public Nullable<HealthSegement> getHealthSegment (int segmentNumber)
    {
        if (segmentNumber >= segmentArray.Length) return null;

        return segmentArray[segmentNumber];
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Returns the HealthSegment currently being modified when applying damage
    public HealthSegement getCurrentHealthSegment ()
    {
        updateTopHealthSegment();
        return segmentArray[currentSegment];
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Returns the array of all health segments
    public HealthSegement[] getSegmentArray ()
    {
        return segmentArray;
    }
    #endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called for when the object dies - runs out of health on lowest layer 
    private void deathEvent ()
    {
        Debug.Log(gameObject.name + " has died!");                          //DEV NOTE - Add your death event here
        gameObject.SetActive(false);
    } 
}

/// <summary>
/// Struct used for holding all relevant data on a health segment. Structs used for access and modification speeds to cope with the likeihood that
/// developers will be using this across a large number of objects and/or making modifications frequently. (Damage over time effects for example)
/// </summary>
[System.Serializable]
public struct HealthSegement
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

/// <summary>
/// Class handles all modification of segment values whenever an event occurs where damage has been taken.
/// </summary>
class DamageControl
{
    private HealthSegement[] segmentArray;
    private HealthController parent;
    private RechargeControl rechargeControl;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Constructor
    public DamageControl(HealthSegement[] segmentArray, RechargeControl rechargeControl, HealthController parent)
    {
        this.segmentArray = segmentArray;
        this.rechargeControl = rechargeControl;
        this.parent = parent;
    }

    #region Damage Modification
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Returns a modified float of the provided "damage" param based on the SegmentType of the provided segment
    internal float getSegmentModifiedDamage (float damage, int index)
    {
        //Return modified damage based on segment type
        switch (segmentArray[index].segmentType)
        {
            case SegmentType.health:
                return damage;
            case SegmentType.armour:
                return modifyArmourDamage(damage, segmentArray[index].armourDamageReduction, segmentArray[index].minimumArmourDamage);
            case SegmentType.shield:
                return modifyShieldDamage(segmentArray[index].constantShieldDamage);
            case SegmentType.barrier:
                return modifyBarrierDamage(damage, segmentArray[index].barrierDamageMitigation);
        }

        //Base return case that should never be reached
        Debug.LogError("HEALTH CONTROLLER SCRIPT - Failed SegmentType Comparison - Segment type could not be determined on segment #" + index.ToString() + " when modying damage to be taken. GameObject: " + parent.gameObject.name);
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
    internal float applyDamageToSegment (float damage, int index)
    {
        //If we have more damage to apply than the segment has health
        if (segmentArray[index].currentHealth < damage)
        {
            //Update damage with the difference and set current health to zero
            damage -= segmentArray[index].currentHealth;
            segmentArray[index].currentHealth = 0;
        }
        //Otherwise we can just apply all damage
        else
        {
            segmentArray[index].currentHealth -= damage;
            damage = 0f;
        }

        //Return the remaining damage if we are carrying to the next segment
        if (segmentArray[index].carryDamageToNextSegment) return damage;
        //Otherwise we will have no damage to continue with
        else return 0f;
    }
}

/// <summary>
/// Class handles all modification of values whenever an event occurs where healing has been applied
/// </summary>
class HealingControl
{
    public HealingControl(HealthSegement[] segArray, HealthController parent)
    {

    }
}

/// <summary>
/// Class handles keeping track of recharge times and modification for all segments during the course of gameplay
/// </summary>
class RechargeControl
{
    public RechargeControl(HealthSegement[] segArray, HealthController parent)
    {

    }
}