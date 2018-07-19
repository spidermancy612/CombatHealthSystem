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
        damageControl = new DamageControl(segmentArray, this);
        healingControl = new HealingControl(segmentArray, this);
        rechargeControl = new RechargeControl(segmentArray, this);

        
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
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called by others scripts to apply damage to this script/object
    //Default public call with no special modifiers for damage
    public void applyDamage(float damage)
    {
        //Apply modified damage to the current top health segment
        updateTopHealthSegment();
        applyDamageToSegment(getModifiedDamageValue(damage, currentSegment), false);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called by others scripts to apply damage to this script/object
    //Call includes a boolean paramter for ignoring segment type modifiers
    public void applyDamage (float damage, bool ignoreMofiers)
    {
        //If we're ignoring modifiers then we go straight to applying damage
        if (ignoreMofiers)
        {
            applyDamageToSegment(damage, true);
        }
        //Otherwise we will modify the damage first using the default method call
        else
        {
            applyDamage(damage);
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called by other scripts to apply damage to this script/object.
    //Call allows for specifying damage to only be applied if the segment matches the provided parameter. Uses a seperate
    //damage application system due to a number of small changes in how damage is applied.
    public void applyDamage (float damage, SegmentType type, bool ignoreModifiers)
    {
        //Iterate through all possible segment from top down
        for (int i = segmentArray.Length - 1; i >= 0; i--)
        {
            //Check if we have a matching segment
            if (segmentArray[i].segmentType == type)
            {
                //If we're going to be carrying damage past the first segment
                if (segmentArray[i].carryDamageToNextSegment)
                {
                    //Modify damage if we need to
                    if (ignoreModifiers == false) damage = getModifiedDamageValue(damage, i);

                    //If we have damage overflow 
                    if (segmentArray[i].currentHealth < damage)
                    {
                        //Modify the remaining damage and set current segment to zero
                        damage -= segmentArray[i].currentHealth;
                        segmentArray[i].currentHealth = 0;
                    }
                    //Otherwise we just apply all the damage here
                    else
                    {
                        //Reduce the health by remaining damage
                        segmentArray[i].currentHealth -= damage;
                        //Stop checking the rest of the segment - saves on performance
                        return;
                    }
                }
                //Otherwise all damage ends here
                else
                {
                    //Modify the damage values if we need to
                    if (ignoreModifiers == false) damage = getModifiedDamageValue(damage, i);

                    //Apply the damage to the current health segment
                    segmentArray[i].currentHealth -= damage;
                    //Make sure value does not go below zero
                    if (segmentArray[i].currentHealth < 0) segmentArray[i].currentHealth = 0;

                    //End damage application here
                    return;
                }
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Handles applying the provided damage value to the current segment for use. Also will track if damage overflow will
    //carried to the next segment if the option/boolean has been enabled
    private void applyDamageToSegment(float damage, bool ignoreModifers)
    {
        //Update the top segment for applying damage and return if we hit a player death case
        updateTopHealthSegment();
        if (currentSegment == -1) return;

        //Update the recharge timer for this segment since it's taking damage
        updateRechargeTimer();
     
        //If we have more health than the damage we're taking we directly apply it OR if this is the base health segment
        if (damage <= segmentArray[currentSegment].currentHealth || currentSegment == 0)
        {
            segmentArray[currentSegment].currentHealth -= damage;
        }
        //Otherwise we have extra damage to apply to the next layer
        else
        {
            //Carry damage to next segment if selected
            if (segmentArray[currentSegment].carryDamageToNextSegment)
            {
                //Set te current segment to 0 and correct for new damage value
                damage -= segmentArray[currentSegment].currentHealth;
                segmentArray[currentSegment].currentHealth = 0;

                //Apply the damage on the new segment
                applyDamage(damage, ignoreModifers);
            }
            //Otherwise all damage is applied here
            else
            {
                segmentArray[currentSegment].currentHealth = 0f;
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Returns the damage modified to be reduced by a set value or returns the minimum damage for an armour layer if the 
    //damage falls below the min threshold
    private float armourDamageModifier(float damage)
    {
        //Modify damage for armour
        damage -= segmentArray[currentSegment].armourDamageReduction;

        //Correct damage value if it dips below the minimum
        if (damage < segmentArray[currentSegment].minimumArmourDamage)
        {
            damage = segmentArray[currentSegment].minimumArmourDamage;
        }

        return damage;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Returns the constant damage a shield will take on each attack
    private float shieldDamageModifier()
    {
        return segmentArray[currentSegment].constantShieldDamage;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Returns damage as a percentage of the original paramter (decimal)
    private float barrierDamageModifier(float damage)
    {
        return damage * segmentArray[currentSegment].barrierDamageMitigation;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called whenever the segment takes damage to reset recharge timer
    //Uses current segment for individual recharge since that segment defines damage and recharge if universal is disabled
    private void updateRechargeTimer ()
    {
        //If we have universal damage reset we reset all recharge timers anytime damage is taken
        if (universalDamageReset)
        {
            //Iterate through all segments
            for (int i = 0; i < segmentArray.Length; i++)
            {
                //Reset recharge timer
                segmentArray[i].rechargeTimer = segmentArray[i].rechargeDelay;
            }
        }
        //Otherwise we're just reseting the current segment
        else
        {
            segmentArray[currentSegment].rechargeTimer = segmentArray[currentSegment].rechargeDelay;
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Returns the modified value for damage to be applied to a segment based on the segmentType.
    private float getModifiedDamageValue (float damage, int index)
    {
        switch (segmentArray[index].segmentType)
        {
            case SegmentType.health:
                return damage;
            case SegmentType.shield:
                return shieldDamageModifier();
            case SegmentType.armour:
                return armourDamageModifier(damage);
            case SegmentType.barrier:
                return barrierDamageModifier(damage);
        }

        //Failed switch case - should never occur
        Debug.LogError("ASSET CODE ERROR - Bad Segment Comparison - HealthController class attached to " + gameObject.name + " was unable to compare the segmentType ENUM on Segment " + index.ToString());
        return damage;
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

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Constructor
    public DamageControl(HealthSegement[] segArray, HealthController p)
    {
        segmentArray = segArray;
        parent = p;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    internal float getSegmentModifiedDamage (float damage, int index)
    {
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