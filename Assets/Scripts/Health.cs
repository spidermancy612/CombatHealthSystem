using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class Health : MonoBehaviour {

    public HealthSegement[] healthSegmentArray;

    //public int numberOfSegments;

    private int currentSegment;
    private int currentRechargeSegment;

    [HideInInspector]
    //public const int SEGMENT_ATTRIBUTE_COUNT = 3;

    private bool canRechargeSegment;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called at start of scene before the first frame and whenever the object is re-enabled
    private void Awake()
    {
        //Log warnings and errors
        errorChecking();
        warningChecking();

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

    #region Log Checking
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    private void errorChecking()
    {
        if (healthSegmentArray.Length == 0)
        {
            Debug.LogError("DEVELOPER ERROR - Array Length - Array for health segments has no elements and will result in this script not working :: " + gameObject.name);
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    private void warningChecking()
    {

    }
    #endregion

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

    #region Apply Damage (External Call)
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called by external scripts to apply damage to this object based on the segment type
    public void applyDamage(float damage)
    {
        updateRechargeTimers();

        //Iterate through all the attributes 
        for (int i = 0; i < healthSegmentArray[currentSegment].segmentAttributes.Length; i++)
        {
            switch (healthSegmentArray[currentSegment].segmentAttributes[i])
            {
                case SegmentAttribute.NONE:
                    break;
                case SegmentAttribute.shield:
                    damage = shieldDamageModifier();
                    break;
                case SegmentAttribute.armour:
                    damage = armourDamageModifier(damage);
                    break;
                case SegmentAttribute.barrier:
                    damage = barrierDamageModifier(damage);
                    break;
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Applies damage directly to the segment without any modification
    private void applyHealthDamage(float damage)
    {
        //If we have more health than the damage we're taking we directly apply it OR if this is the base health segment
        if (damage <= healthSegmentArray[currentSegment].currentHealth || currentSegment == 0)
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
                applyDamage(damage);
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
    //Applies damage to the segment reduced by the armour value
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
    //Applies a constant amount of damage to the segment
    private float shieldDamageModifier()
    {
        return healthSegmentArray[currentSegment].constantShieldDamage;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Applies damage to the segment as a percentage of the original amount
    private float barrierDamageModifier(float damage)
    {
        return damage * healthSegmentArray[currentSegment].barrierDamageMitigation;
    }
    #endregion

    #region Apply Damage - Special Calls (External Calls)
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Used to apply bonus damage to a segment if the provided attribute matches
    public void applyBonusDamage (float baseDamage, float bonusDamage, SegmentAttribute attribute)
    {
        if (segmentHasAttribute(attribute))
        {
            applyDamage(baseDamage + bonusDamage);
        }
        else
        {
            applyDamage(baseDamage);
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Used to apply bonus damage to a segment if one of the provided attributes matches
    public void applyBonusDamage(float baseDamage, float bonusDamage, SegmentAttribute attribute1, SegmentAttribute attribute2)
    {
        if (segmentHasAttribute(attribute1) || segmentHasAttribute(attribute2))
        {
            applyDamage(baseDamage + bonusDamage);
        }
        else
        {
            applyDamage(baseDamage);
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Method returns if the provided attribute is selected on the current health segment for applying damage
    private bool segmentHasAttribute (SegmentAttribute attribute)
    {
        //Update current top health segment
        updateTopHealthSegment();

        //Check each health segment attribute for the desired attribute
        for (int i = 0; i < healthSegmentArray[currentSegment].segmentAttributes.Length; i++)
        {
            //If we find a match return true
            if (healthSegmentArray[currentRechargeSegment].segmentAttributes[i] == attribute)
            {
                return true;
            }
        }

        //Base case if no matches are found, return false
        return false;
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

    [Header("Start Options")]
    public float maxHealth;
    public bool startActive;   
    public bool carryDamageToNextSegment;
    [Space(3)]
    public SegmentAttribute[] segmentAttributes;

    [Space(5)]
    [Header("Recharge Options")]
    public bool canRecharge;
    public bool damageResetsSegment;
    public bool anyDamageResetsSegment;
    public float rechargeRate;  
    public float rechargeDelay;

    [Space(5)]
    [Header("Armour Options")]
    public float armourDamageReduction; 
    public float minimumArmourDamage;   

    [Space(5)]
    [Header("Shield Options")]
    public float constantShieldDamage; 

    [Space(5)]
    [Header("Barrier Options")]
    public float barrierDamageMitigation;  


    public float currentHealth;   
    public float rechargeTimer;  

}

public enum SegmentAttribute { armour, shield, barrier, NONE }