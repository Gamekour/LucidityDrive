using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Inputs : MonoBehaviour
{
    [SerializeField] InputActionAsset m_ActionAsset;
    public InputActionAsset actionAsset{ get => m_ActionAsset; set => m_ActionAsset = value; }

    private void Awake()
    {
        // i know this is awful but i don't know how to do it the right way

        InputManager.actionAsset = actionAsset;
        InputManager.movement = actionAsset.FindAction("movement", true);
        InputManager.headlook = actionAsset.FindAction("headlook", true);
        InputManager.jump = actionAsset.FindAction("jump", true);
        InputManager.crouch = actionAsset.FindAction("crouch", true);
        InputManager.sprint = actionAsset.FindAction("sprint", true);
        InputManager.slide = actionAsset.FindAction("slide", true);
        InputManager.crawl = actionAsset.FindAction("crawl", true);
        InputManager.mousepos = actionAsset.FindAction("mousepos", true);
        InputManager.grabL = actionAsset.FindAction("grabL", true);
        InputManager.grabR = actionAsset.FindAction("grabR", true);
        InputManager.equip0 = actionAsset.FindAction("equip0", true);
        InputManager.equip1 = actionAsset.FindAction("equip1", true);
        InputManager.equip2 = actionAsset.FindAction("equip2", true);
        InputManager.equip3 = actionAsset.FindAction("equip3", true);
        InputManager.equipScroll = actionAsset.FindAction("equipScroll", true);
        InputManager.camselect1 = actionAsset.FindAction("camselect1", true);
        InputManager.camselect2 = actionAsset.FindAction("camselect2", true);
        InputManager.camselect3 = actionAsset.FindAction("camselect3", true);
        InputManager.camselect4 = actionAsset.FindAction("camselect4", true);
        InputManager.mouseUnlock = actionAsset.FindAction("mouseUnlock", true);
        InputManager.reload = actionAsset.FindAction("reload", true);
        InputManager.teamSelect = actionAsset.FindAction("teamSelect", true);
        InputManager.classSelect = actionAsset.FindAction("classSelect", true);
        InputManager.gunBash = actionAsset.FindAction("gunBash", true);
    }

    private void OnEnable()
    {
        if (m_ActionAsset != null)
        {
            m_ActionAsset.Enable();
        }
    }
}

public static class InputManager
{
    public static InputActionAsset actionAsset;
    public static InputAction movement;
    public static InputAction jump;
    public static InputAction crouch;
    public static InputAction sprint;
    public static InputAction headlook;
    public static InputAction slide;
    public static InputAction crawl;
    public static InputAction mousepos;
    public static InputAction grabL;
    public static InputAction grabR;
    public static InputAction equip0;
    public static InputAction equip1;
    public static InputAction equip2;
    public static InputAction equip3;
    public static InputAction equipScroll;
    public static InputAction camselect1;
    public static InputAction camselect2;
    public static InputAction camselect3;
    public static InputAction camselect4;
    public static InputAction mouseUnlock;
    public static InputAction reload;
    public static InputAction teamSelect;
    public static InputAction classSelect;
    public static InputAction gunBash;
}
