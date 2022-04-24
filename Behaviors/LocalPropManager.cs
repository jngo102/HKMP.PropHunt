using Hkmp.Api.Client.Networking;
using PropHunt.HKMP;
using UnityEngine;

namespace PropHunt.Behaviors
{
    internal enum PropState
    {
        Free,
        TranslateXY,
        TranslateZ,
        Rotate,
    }

    internal class LocalPropManager : MonoBehaviour
    {
        private const float TRANSLATE_XY_SPEED = 0.5f;
        private const float TRANSLATE_Z_SPEED = 0.5f;
        private const float ROTATE_SPEED = 10;

        private const float MIN_Z = -1;
        private const float MAX_Z = 1;
        private const float XY_MAX_MAGNITUDE = 1;

        private HeroController _hc;
        private InputHandler _input;
        private MeshRenderer _meshRend;
        private GameObject _username;
        public GameObject Prop { get; private set; }
        private IClientAddonNetworkSender<FromClientToServerPackets> _sender;
        private SpriteRenderer _propSprite;
        public Sprite PropSprite => _propSprite.sprite;
        private PropState _propState = PropState.Free;

        private void Awake()
        {
            _hc = HeroController.instance;
            _input = GameManager.instance.inputHandler;
            _meshRend = GetComponent<MeshRenderer>();
            _username = transform.Find("Username").gameObject;  
            Prop = new GameObject("Prop");
            Prop.transform.SetParent(transform);
            Prop.transform.localPosition = Vector3.zero;
            _propSprite = Prop.AddComponent<SpriteRenderer>();
            _sender = PropHuntClient.Instance.PropHuntClientApi.NetClient.GetNetworkSender<FromClientToServerPackets>(PropHuntClient.Instance);
        }

        private void Update()
        {
            ReadPropStateInputs();
            ReadMovementInputs(); 
        }

        private void ReadPropStateInputs()
        {
            if (Input.GetKeyDown(PropHunt.Instance.Settings.TranslateXYKey))
            {
                if (_propState != PropState.TranslateXY)
                {
                    PropHunt.Instance.Log("Translate XY");
                    _propState = PropState.TranslateXY;
                    _hc.IgnoreInput();
                }
                else
                {
                    _propState = PropState.Free;
                    _hc.AcceptInput();
                }
            }

            if (Input.GetKeyDown(PropHunt.Instance.Settings.TranslateZKey))
            {
                if (_propState != PropState.TranslateZ)
                {
                    PropHunt.Instance.Log("Translate Z");
                    _propState = PropState.TranslateZ;
                    _hc.IgnoreInput();
                }
                else
                {
                    _propState = PropState.Free;
                    _hc.AcceptInput();
                }
            }

            if (Input.GetKeyDown(PropHunt.Instance.Settings.RotateKey))
            {
                if (_propState != PropState.Rotate)
                {
                    PropHunt.Instance.Log("Rotate");
                    _propState = PropState.Rotate;
                    _hc.IgnoreInput();
                }
                else
                {
                    _propState = PropState.Free;
                    _hc.AcceptInput();
                }
            }

            if (Input.GetKeyDown(PropHunt.Instance.Settings.SelectKey))
            {
                PropHunt.Instance.Log("Select a prop");
                float shortestDistance = 1;
                Breakable closestBreakable = null;
                foreach (var breakable in FindObjectsOfType<Breakable>())
                {
                    float distance = Vector2.Distance(breakable.transform.position, HeroController.instance.transform.position);
                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                        closestBreakable = breakable;
                    }
                }

                if (closestBreakable == null) return;

                _propState = PropState.Free;

                var breakSprite = closestBreakable.GetComponentInChildren<SpriteRenderer>().sprite;
                _meshRend.enabled = false;
                _propSprite.sprite = breakSprite;
                _sender.SendSingleData
                (
                    FromClientToServerPackets.BroadcastPropSprite,
                    new PropSpriteFromClientToServerData
                    {
                        SpriteName = breakSprite.name,
                    }
                );
            }
        }

        private void ReadMovementInputs()
        {
            switch (_propState)
            {
                case PropState.TranslateXY:
                    Prop.transform.localPosition += (Vector3)_input.inputActions.moveVector.Value * Time.deltaTime * TRANSLATE_XY_SPEED;
                    var clampedVector = Vector2.ClampMagnitude(Prop.transform.localPosition, XY_MAX_MAGNITUDE);
                    var newPos = new Vector3(clampedVector.x, clampedVector.y, Prop.transform.localPosition.z);
                    Prop.transform.localPosition = newPos;
                    _sender.SendSingleData
                    (
                        FromClientToServerPackets.BroadcastPropPositionXY,
                        new PropPositionXYFromClientToServerData
                        {
                            PositionXY = new Hkmp.Math.Vector2(Prop.transform.localPosition.x, Prop.transform.localPosition.y),
                        }
                    );
                    break;
                case PropState.TranslateZ:
                    var moveZ = Vector3.forward * _input.inputActions.moveVector.Value.y * Time.deltaTime * TRANSLATE_Z_SPEED;
                    Prop.transform.localPosition += moveZ;
                    newPos = new Vector3(Prop.transform.localPosition.x, Prop.transform.localPosition.y, Mathf.Clamp(Prop.transform.localPosition.z, MIN_Z, MAX_Z));
                    Prop.transform.localPosition = newPos;
                    _sender.SendSingleData
                    (
                        FromClientToServerPackets.BroadcastPropPositionZ,
                        new PropPositionZFromClientToServerData
                        {
                            PositionZ = Prop.transform.localPosition.z,
                        }
                    );
                    break;
                case PropState.Rotate:
                    var rotateZ = _input.inputActions.moveVector.Value.x * Time.deltaTime * ROTATE_SPEED;
                    Prop.transform.Rotate(0, 0, rotateZ);
                    _sender.SendSingleData
                    (
                        FromClientToServerPackets.BroadcastPropRotation,
                        new PropRotationFromClientToServerData
                        {
                            Rotation = Prop.transform.rotation.eulerAngles.z,
                        }
                    );
                    break;
                default:
                    break;
            }
        }
    }
}