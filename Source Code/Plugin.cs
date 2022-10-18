using BepInEx;
using System;
using System.IO;
using System.Collections;
using System.Reflection;
using Utilla;
using UnityEngine;
using UnityEngine.XR;

namespace GorillaPistol
{
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.5.0")]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        static GameObject Gun;
        public static GameObject handCollider;
        static bool RightTriggerDown = false;
        static bool RightTriggerCooldown = false;
        static bool RightSecondaryDown2 = false;
        static bool RightSecondaryCooldown2 = false;
        public static bool RightGripDown = false;
        public static bool RightGripCooldown = false;
        public static bool GrabbingGun = true;
        public static bool canPick = false;

        public IEnumerator CanPickTimer()
        {
            canPick = false;
            yield return new WaitForSeconds(0.75f);
            canPick = true;
            yield break;
        }

        public IEnumerator ShootEffect()
        {
            GorillaTagger.Instance.StartVibration(false, 0.15f, 0.075f);
            Gun.transform.Find("Effect").GetComponent<MeshRenderer>().enabled = false;
            yield return new WaitForSeconds(0.05f);
            Gun.transform.Find("Effect").GetComponent<MeshRenderer>().enabled = false;
            yield break;
        }

        void OnEnable()
        {
            Events.GameInitialized += OnGameInitialized;
            if (Gun != null)
            {
                Gun.SetActive(true);
            }
        }

        void OnDisable()
        {
            Events.GameInitialized -= OnGameInitialized;
            if (Gun != null)
            {
                Gun.SetActive(false);
            }
        }

        void OnGameInitialized(object sender, EventArgs e)
        {
            GameObject RightPalm = GorillaTagger.Instance.offlineVRRig.rightHandTransform.parent.Find("palm.01.R").gameObject;
            Stream AssetBundleStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GorillaPistol.Resources.pistol");
            AssetBundle AssetBundle = AssetBundle.LoadFromStream(AssetBundleStream);
            GameObject Object = AssetBundle.LoadAsset<GameObject>("pistol");
            Gun = Instantiate(Object) as GameObject;

            Gun.transform.SetParent(RightPalm.transform, false);
            Gun.transform.localPosition = new Vector3(-0.0384f, -0.003f, -0.016f);
            Gun.transform.localRotation = Quaternion.Euler(-88.25301f, 0f, 2.632f);
            Gun.transform.localScale = new Vector3(0.81586f, 0.81586f, 0.81586f);
            Gun.SetActive(true);

            Gun.transform.Find("Effect").GetComponent<MeshRenderer>().enabled = false;
        }

        void CreateBullet()
        {

            GameObject Bullet = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            GameObject.Destroy(Bullet.GetComponent<CapsuleCollider>());
            Bullet.transform.SetParent(Gun.transform, false);
            Bullet.layer = 8;
            Rigidbody rb = Bullet.AddComponent<Rigidbody>();
            SphereCollider nC = Bullet.AddComponent<SphereCollider>();
            nC.isTrigger = true;
            rb.useGravity = true;
            rb.mass = 1.5f;
            Bullet.transform.localPosition = new Vector3(-0.0001f, 0.0757f, 0.2584f);
            Bullet.transform.localRotation = Quaternion.Euler(93.586f, 45.73599f, 45.79799f);
            Bullet.transform.localScale = new Vector3(0.01739963f * 1.2f, 0.01739963f * 1.2f, 0.01739963f * 1.2f);
            Bullet.GetComponent<Renderer>().material.SetColor("_Color", new Color(0.6611339f, 0.7304736f, 0.7830189f, 1f));
            Bullet.transform.SetParent(null, true);

            Bullet.AddComponent<Bullet>();

            rb.AddRelativeForce(Vector3.up * 65f, ForceMode.Impulse);
            rb.AddRelativeForce(Vector3.back * 2f, ForceMode.Impulse);
        }

        public void DropGun()
        {
            GrabbingGun = false;
            Gun.transform.Find("lower").gameObject.layer = 8;
            Gun.transform.Find("lower").gameObject.AddComponent<BoxCollider>();

            handCollider = new GameObject();
            handCollider.transform.SetParent(Gun.transform, false);
            handCollider.transform.localPosition = Vector3.zero;
            handCollider.transform.localRotation = Quaternion.identity;
            handCollider.transform.localScale = Vector3.one;
            handCollider.layer = 0;
            BoxCollider handColliderBox = handCollider.AddComponent<BoxCollider>();
            handColliderBox.size = Gun.transform.Find("lower").gameObject.transform.localScale + Gun.transform.Find("lower").gameObject.transform.localScale / 1.5f; //bigger collider
            handCollider.AddComponent<GunPick>();
            Gun.transform.SetParent(null, true);

            Rigidbody rb = Gun.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.velocity = new Vector3(0, -0.1f, 0);
        }

        public static void PickUpGun()
        {
            GameObject RightPalm = GorillaTagger.Instance.offlineVRRig.rightHandTransform.parent.Find("palm.01.R").gameObject;
            if (handCollider != null)
            {
                GameObject.Destroy(handCollider);
            }
            GameObject.Destroy(Gun.GetComponent<Rigidbody>());
            GameObject.Destroy(Gun.transform.Find("lower").gameObject.GetComponent<BoxCollider>());
            Gun.transform.Find("lower").gameObject.layer = 0;
            GrabbingGun = true;
            Gun.transform.SetParent(RightPalm.transform, true);

            LeanTween.moveLocal(Gun, new Vector3(-0.0384f, -0.003f, -0.016f), 0.5f).setEaseOutBack();
            LeanTween.rotateLocal(Gun, new Vector3(-88.25301f, 0f, 2.632f), 0.5f).setEaseOutBack();
        }

        void FixedUpdate()
        {
            InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.triggerButton, out RightTriggerDown);
            InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.secondaryButton, out RightSecondaryDown2);
            InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.gripButton, out RightGripDown);

            if (Gun.transform.localPosition.y < -59 && !GrabbingGun)
            {
                PickUpGun(); // if the gun falls out the map it'll bring it back to the player.
            }

            if (Gun.activeInHierarchy == true)
            {
                if (RightTriggerDown && !RightTriggerCooldown && GrabbingGun)
                {
                    RightTriggerCooldown = true;
                    Gun.transform.Find("Shoot").GetComponent<AudioSource>().Play();
                    CreateBullet();
                    StartCoroutine(ShootEffect());
                }
                else
            if (!RightTriggerDown && RightTriggerCooldown)
                {
                    RightTriggerCooldown = false;
                }

                if (RightSecondaryDown2 && !RightSecondaryCooldown2 && GrabbingGun)
                {
                    RightSecondaryCooldown2 = true;
                    DropGun();

                    StartCoroutine(CanPickTimer());
                }
                else
                if (!RightSecondaryDown2 && RightSecondaryCooldown2 && GrabbingGun)
                {
                    RightSecondaryCooldown2 = false;
                }
            }
        }

    }

    public class Bullet : MonoBehaviour
    {
        void Start()
        {
            StartCoroutine(Despawn());
        }

        public IEnumerator Despawn()
        {
            yield return new WaitForSeconds(5f);
            Destroy(gameObject);
        }
    }

    public class GunPick : MonoBehaviour
    {
        void Start()
        {
            gameObject.layer = 10;
            gameObject.GetComponent<BoxCollider>().isTrigger = true;
        }
        public IEnumerator Raise()
        {
            yield return new WaitForSeconds(0.85f);
            Plugin.canPick = true;
            yield break;
        }

        void LateUpdate()
        {
            float dist = Vector3.Distance(GorillaLocomotion.Player.Instance.rightHandTransform.position, transform.position);
            if (dist < 0.25f)
            {
                if (Plugin.canPick && !Plugin.GrabbingGun && Plugin.RightGripDown)
                {
                    StartCoroutine(Raise());
                    Plugin.PickUpGun();
                }
            }
        }
    }
}
