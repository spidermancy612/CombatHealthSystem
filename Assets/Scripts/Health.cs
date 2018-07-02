using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class Health : MonoBehaviour {

    public HealthSegement[] healthSegmentArray;

    private int currentSegment;
    private int currentRechargeSegment;

    private bool canRechargeSegment;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called at start of scene before the first frame and whenever the object is re-enabled
    private void Awake()
    {
        canRechargeSegment = false;
        for (int i = 0; i < healthSegmentArray.Length; i++)
        {
            //Marks that recharge checks should occur
            if (healthSegmentArray[i].canRecharge) canRechargeSegment = true;

            //Make sure all health starts max if option selected
            if (healthSegmentArray[i].startActive)
            {
                healthSegmentArray[i].currentHealth = healthSegmentArray[i].maxHealth;
                healthSegmentArray[i].rechargeTimer = healthSegmentArray[i].rechargeDelay;
            }
        }
    }

    #region Update States and Values
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called every frame while the component is active
    private void Update()
    {
        //Update which segment should be tracked for recharging next
        if (canRechargeSegment)
        {
            updateTopRechargeSegment();
            updateSegmentRecharge();
        }

        //Update which segment will be taking damage next
        updateTopHealthSegment();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called on Update to keep track of the current segment of health to take damage
    private void updateTopHealthSegment ()
    {
        for (int i = 0; i < healthSegmentArray.Length; i++)
        {
            if (healthSegmentArray[i].currentHealth > 0)
            {
                currentSegment = i;
                return;
            }
        }
        //All segments have no health - object is dead
        deathEvent();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called on Update to keep track of what segment should be recharging next
    private void updateTopRechargeSegment ()
    {
        for (int i = healthSegmentArray.Length - 1; i >= 0; i--)
        {
            if (healthSegmentArray[i].currentHealth < healthSegmentArray[i].maxHealth && healthSegmentArray[i].canRecharge)
            {
                currentRechargeSegment = i;
                return;
            }
        }
        //Fall off case where there is nothing to recharge
        currentRechargeSegment = -1;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called on Update to update the recharge timers 
    private void updateSegmentRecharge ()
    {
        if (canRechargeSegment == false || currentRechargeSegment == -1) return;

        //We can recharge the current segment - mainly for error prevention
        if ((healthSegmentArray[currentRechargeSegment].currentHealth < healthSegmentArray[currentRechargeSegment].maxHealth))
        {
            //We need to decriment the timer
            if (healthSegmentArray[currentRechargeSegment].rechargeTimer > 0)
            {
                healthSegmentArray[currentRechargeSegment].rechargeTimer -= Time.deltaTime;
            }
            //Otherwise timer expired and we recharge health
            else
            {
                healthSegmentArray[currentRechargeSegment].currentHealth += Time.deltaTime * healthSegmentArray[currentRechargeSegment].rechargeRate;
            }

            //Make sure current health does not exceed max health
            if (healthSegmentArray[currentRechargeSegment].currentHealth > healthSegmentArray[currentRechargeSegment].maxHealth)
            {
                healthSegmentArray[currentRechargeSegment].currentHealth = healthSegmentArray[currentRechargeSegment].maxHealth;
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called to update the timer on each segment if it can recharge
    private void updateRechargeTimers ()
    {
        //Iterate through all segments
        for (int i = 0; i < healthSegmentArray.Length; i++)
        {
            //Reset the timer if it is marked to do so whenever damage is applied
            if (healthSegmentArray[i].anyDamageResetsSegment)
            {
                healthSegmentArray[i].rechargeTimer = healthSegmentArray[i].rechargeDelay;
            }        
        }
        //Make sure the current segment gets updated regardless
        if (healthSegmentArray[currentRechargeSegment].damageResetsSegment && healthSegmentArray[currentRechargeSegment].anyDamageResetsSegment == false)
        {
            healthSegmentArray[currentRechargeSegment].rechargeTimer = healthSegmentArray[currentRechargeSegment].rechargeDelay;
        }
    }
    #endregion

    #region Apply Damage
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called by external scripts to apply damage to this object based on the segment type
    public void applyDamage(float damage)
    {
        updateRechargeTimers();

        switch (healthSegmentArray[currentSegment].segmentType)
        {
            case SegmentType.health:
                applyDamageToSegment(damage, false);
                break;
            case SegmentType.shield:
                damage = shieldDamageModifier();
                applyDamageToSegment(damage, false);
                break;
            case SegmentType.armour:
                damage = armourDamageModifier(damage);
                applyDamageToSegment(damage, false);
                break;
            case SegmentType.barrier:
                damage = barrierDamageModifier(damage);
                applyDamageToSegment(damage, false);
                break;
        }
        
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called to apply damage to the segments with the additional option to avoid segment damage modifiers
    public void applyDamage (float damage, bool ignoreMofiers)
    {
        if (ignoreMofiers)
        {
            applyDamageToSegment(damage, true);
        }
        else
        {
            applyDamage(damage);
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Applies damage directly to the segment without any modification
    private void applyDamageToSegment(float damage, bool ignoreModifers)
    {
        //If we have more health than the damage we're taking we directly apply it OR if this is the base health segment
        if (damage <= healthSegmentArray[currentSegment].currentHealth || currentSegment == healthSegmentArray.Length - 1)
        {
            healthSegmentArray[currentSegment].currentHealth -= damage;
        }
        //Otherwise we have extra damage to apply to the next layer
        else
        {
            //Carry damage to next segment if selected
            if (healthSegmentArray[currentSegment].carryDamageToNextSegment)
            {
                //Set te current segment to 0 and correct for new damage value
                damage -= healthSegmentArray[currentSegment].currentHealth;
                healthSegmentArray[currentSegment].currentHealth = 0;

                //Update to the new segment we're damaging
                updateTopHealthSegment();

                //Apply the damage on the new segment
                applyDamage(damage, ignoreModifers);
            }
            //Otherwise all damage is applied here
            else
            {
                healthSegmentArray[currentSegment].currentHealth = 0f;
                updateTopHealthSegment();
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Returns the damage modified to be reduced by a set value or returns the minimum damage for an armour layer if the 
    //damage falls below the min threshold
    private float armourDamageModifier(float damage)
    {
        //Modify damage for armour
        damage -= healthSegmentArray[currentSegment].armourDamageReduction;
        if (damage < healthSegmentArray[currentSegment].minimumArmourDamage)
        {
            damage = healthSegmentArray[currentSegment].minimumArmourDamage;
        }

        return damage;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Returns the constant damage a shield will take on each attack
    private float shieldDamageModifier()
    {
        return healthSegmentArray[currentSegment].constantShieldDamage;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Returns damage as a percentage of the original paramter 
    private float barrierDamageModifier(float damage)
    {
        return damage * healthSegmentArray[currentSegment].barrierDamageMitigation;
    }
    #endregion

    #region Apply Damage - Special Calls
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Applies additional damage if the current segment matches the provided paramter
    public void applyBonusDamage(float baseDamage, float bonusDamage, SegmentType type, bool ignoreModifiers)
    {
        //Make sure we're on the right segment
        updateTopHealthSegment();

        //Check if we match with the provided segment type
        if (healthSegmentArray[currentSegment].segmentType == type)
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
    //Applies additional damage if the current segment contains the provided string paramter
    public void applyBonusDamage (float baseDamage, float bonusDamage, string tag, bool ignoreModifiers)
    {
        updateTopHealthSegment();

        //Check for the array not existing for some reason
        if (healthSegmentArray[currentSegment].specialTags == null)
        {
            Debug.LogError("ASSET WARNING - Null Array - Tags array for " + gameObject.name + " has no tags array attached and will prevent any tag calls from being made! Please contact creator if you see this message.");
            return;
        }
        //Check if they've called for a tag to be used but no tag is set
        if (healthSegmentArray[currentSegment].specialTags[0] == "")
        {
            Debug.LogWarning("DEVELOPER WARNING - Bad Method Call - Attempted to call applyBonusDamage on " + gameObject.name + " using a tag when no tags have been set for the associated health segment." +
                "Please check segment #" + currentSegment.ToString() + " for tags to correct this warning.");
            return;
        }

        //Check each string in the tags array
        foreach (string t in healthSegmentArray[currentRechargeSegment].specialTags)
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
    //Applies additional damage if the current segment tags contains any of the provided string paramters
    public void applyBonusDamage(float baseDamage, float bonusDamage, string[] tagArray, bool ignoreModifiers)
    {
        updateTopHealthSegment();

        //Check for the array not existing for some reason
        if (healthSegmentArray[currentSegment].specialTags == null)
        {
            Debug.LogError("ASSET WARNING - Null Array - Tags array for " + gameObject.name + " has no tags array attached and will prevent any tag calls from being made! Please contact creator if you see this message.");
            return;
        }
        //Check if they've called for a tag to be used but no tag is set
        if (healthSegmentArray[currentSegment].specialTags[0] == "")
        {
            Debug.LogWarning("DEVELOPER WARNING - Bad Method Call - Attempted to call applyBonusDamage on " + gameObject.name + " using a tag when no tags have been set for the associated health segment." +
                "Please check segment #" + currentSegment.ToString() + " for tags to correct this warning.");
            return;
        }

        //Check each string in the tags array
        foreach (string st in healthSegmentArray[currentRechargeSegment].specialTags)
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
    //Public method to be called when adding health to a segment. Will scan from lowest to highest segment to add health 
    //to the first segment matching the SegmentType paramter
    public void applyHealth (float health, SegmentType type)
    {
        addHealth(health, findSegmentForAddingHealth(type), type);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Adds health to the segment at the provided index and handles health overflow to other segments when needed
    private void addHealth (float health, int index, SegmentType type)
    {
        if (index == -1) return;

        //If we have more health than we can heal on this segment
        if (health + healthSegmentArray[index].currentHealth > healthSegmentArray[index].maxHealth)
        {
            //Save the overflow of health and set current health to max
            health -= healthSegmentArray[index].maxHealth;
            healthSegmentArray[index].currentHealth = healthSegmentArray[index].maxHealth;

            //If carry over is enabled we search for the next matching segment and provide it the overflow health
            if (healthSegmentArray[index].carryHealingToNextSegment)
            {
                applyHealth(health, type);
            }
        }
        //Otherwise we can normally apply health to this segment
        else
        {
            healthSegmentArray[index].currentHealth += health;
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Scanes through all segments from the lowest to highest level (greatest index to lowest) and returns the segment index
    //if the segment is missing health and matches the SegmentType provided
    private int findSegmentForAddingHealth (SegmentType type)
    {
        //Check through all segment from the bottom up
        for(int i = healthSegmentArray.Length - 1; i >= 0; i--)
        {
            //If the SegmentTypes match and the segment is missing health, return the segment index
            if (type == healthSegmentArray[i].segmentType && healthSegmentArray[i].currentHealth < healthSegmentArray[i].maxHealth)
            {
                return i;
            }
        }

        //We never found a match so return -1
        return -1;
    }
    #endregion

    #region Getter Methods
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Getter method for providing the current health of all segments
    public float[] getAllHealthValues ()
    {
        float[] temp = new float[healthSegmentArray.Length];

        for (int i = 0; i < healthSegmentArray.Length; i++)
        {
            temp[i] = healthSegmentArray[i].currentHealth;
        }

        return temp;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Getter method for finding the number of segments tracked in this class
    public int getNumberOfSegments ()
    {
        return healthSegmentArray.Length;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Returns the HealthSegment struct at the specified index within the healthSegmentArray
    public Nullable<HealthSegement> getHealthSegment (int segmentNumber)
    {
        if (segmentNumber >= healthSegmentArray.Length) return null;

        return healthSegmentArray[segmentNumber];
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Returns the HealthSegment currently being modified when applying damage
    public HealthSegement getCurrentHealthSegment ()
    {
        updateTopHealthSegment();
        return healthSegmentArray[currentSegment];
    }
    #endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called for when the object dies - runs out of health on lowest layer 
    private void deathEvent ()
    {
        Debug.Log("DIEDED!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
    } 
}

[System.Serializable]
public struct HealthSegement
{
#if UNITY_EDITOR
    public string segmentName;
#endif

    public float maxHealth;
    public bool startActive;   

    public bool carryDamageToNextSegment;
    public bool carryHealingToNextSegment;

    public SegmentType segmentType;
    public string[] specialTags;

    public bool canRecharge;
    public bool damageResetsSegment;
    public bool anyDamageResetsSegment;
    public float rechargeRate;  
    public float rechargeDelay;

    public float armourDamageReduction; 
    public float minimumArmourDamage;   

    public float constantShieldDamage; 

    public float barrierDamageMitigation;  


    public float currentHealth;   
    public float rechargeTimer;  

}

public enum SegmentType { health, armour, shield, barrier }