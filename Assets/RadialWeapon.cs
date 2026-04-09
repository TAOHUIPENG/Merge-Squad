using D2D;
using D2D.Core;
using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static D2D.Utilities.CommonGameplayFacade;

public class RadialWeapon : MonoBehaviour
{
    [SerializeField] private GameObject radialWeaponPrefab;
    [SerializeField] private FormationComponent formation;

    private List<GameObject> weapons = new List<GameObject>();

    private bool isEnabled;

    private float timer;

    private Tween scaleTween;

    private void Awake()
    {
        _radialWeapon = this;

        // Stop cycling weapons on game end so they no longer spawn projectile VFX
        // (NovaLightningBlue etc.) whose DOTweenAnimation targets can be null in
        // WebGL, leading to invalid WASM function-table calls and a hard crash.
        _stateMachine.On<WinState>(() => enabled = false);
        _stateMachine.On<LoseState>(() => enabled = false);
    }

    private void Update()
    {
        if (timer <= Time.time)
        {
            if (isEnabled)
            {
                EnableWeapon(false);

                timer = Time.time + _gameData.radialWeaponCooldown;
            }
            else
            {
                EnableWeapon(true);

                timer = Time.time + _gameData.radialWeaponDuration;
            }
        }
    }

    [Button]
    public void AddWeapon()
    {
        scaleTween.KillTo0();
        scaleTween = transform.DOScale(1, 0.5f);
        
        var weaponGO = Instantiate(radialWeaponPrefab, transform);
        weapons.Add(weaponGO);
        formation.PlaceAdditionalPointsInFormation(2f, weapons.Count);

        for (int i = 0; i < weapons.Count; i++)
        {
            GameObject weapon = weapons[i];

            weapon.transform.localPosition = formation.FormationPoints[i].transform.localPosition;
        }

        if (weapons.Count == 1)
        {
            AddWeapon();
        }
    }
    private void EnableWeapon(bool enable)
    {
        scaleTween.KillTo0();

        scaleTween = transform.DOScale(enable ? 1 : 0, 0.5f);
        isEnabled = enable;
    }
}