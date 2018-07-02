using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleHealthController : MonoBehaviour {

    #region Health Options Variables
    //Type
    internal HealthTypeEnum healthType;

    //Health
    internal float health;
    internal float maxHealth;

    //Booleans
    internal bool useSegment;
    internal bool startFilled;
    internal bool canRecharge;
    internal bool damageResetsRecharge;

    //Recharge
    internal float rechargeDelay;
    internal float rechargeSpeed;
    internal float rechargeTimer;

    //Armour
    internal float armourDamageReduction;
    internal float minimumArmourDamage;

    //Shield
    internal float constantShieldDamage;

    //Barrier
    internal float barrierDamageMitigation;
    #endregion

    // Use this for initialization
    void Start () {
        
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
