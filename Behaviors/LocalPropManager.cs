using Hkmp.Api.Client.Networking;
using PropHunt.HKMP;
using UnityEngine;

using HKMPVector2 = Hkmp.Math.Vector2;

namespace PropHunt.Behaviors
{
    internal enum PropState
    {
        Free,
        TranslateXY,
        TranslateZ,
        Rotate,
        Scale,
    }

    internal class LocalPropManager : MonoBehaviour
    {
        private const float TRANSLATE_XY_SPEED = 1f;
        private const float TRANSLATE_Z_SPEED = 1f;
        private const float ROTATE_SPEED = 20f;
        private const float SCALE_SPEED = 0.5f;

        private const float XY_MAX_MAGNITUDE = 0.65f;
        private const float MIN_Z = -0.5f;
        private const float MAX_Z = 0.5f;
        private const float MIN_SCALE = 0.75f;
        private const float MAX_SCALE = 1.5f;

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
            if (PropSprite != null)
            {
                if (Input.GetKeyDown(PropHunt.Instance.Settings.TranslateXYKey))
                {
                    if (_propState != PropState.TranslateXY)
                    {
                        PropHunt.Instance.Log("Translate XY");
                        _propState = PropState.TranslateXY;
                        _hc.IgnoreInput();
                        _hc.RelinquishControl();
                    }
                    else
                    {
                        _propState = PropState.Free;
                        _hc.AcceptInput();
                        _hc.RegainControl();
                    }
                }

                if (Input.GetKeyDown(PropHunt.Instance.Settings.TranslateZKey))
                {
                    if (_propState != PropState.TranslateZ)
                    {
                        PropHunt.Instance.Log("Translate Z");
                        _propState = PropState.TranslateZ;
                        _hc.IgnoreInput();
                        _hc.RelinquishControl();
                    }
                    else
                    {
                        _propState = PropState.Free;
                        _hc.AcceptInput();
                        _hc.RegainControl();
                    }
                }

                if (Input.GetKeyDown(PropHunt.Instance.Settings.RotateKey))
                {
                    if (_propState != PropState.Rotate)
                    {
                        PropHunt.Instance.Log("Rotate");
                        _propState = PropState.Rotate;
                        _hc.IgnoreInput();
                        _hc.RelinquishControl();
                    }
                    else
                    {
                        _propState = PropState.Free;
                        _hc.AcceptInput();
                        _hc.RegainControl();
                    }
                }

                if (Input.GetKeyDown(PropHunt.Instance.Settings.ScaleKey))
                {
                    if (_propState != PropState.Scale)
                    {
                        PropHunt.Instance.Log("Scale");
                        _propState = PropState.Scale;
                        _hc.IgnoreInput();
                        _hc.RelinquishControl();
                    }
                    else
                    {
                        _propState = PropState.Free;
                        _hc.AcceptInput();
                        _hc.RegainControl();
                    }
                }
            }

            if (!Input.GetKeyDown(PropHunt.Instance.Settings.SelectKey)) return;

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

            _propState = PropState.Free;

            string spriteName = "";
            var breakSprite = closestBreakable?.GetComponentInChildren<SpriteRenderer>().sprite;
            _propSprite.sprite = breakSprite;
            if (breakSprite != null)
            {
                spriteName = breakSprite.name;
                _propSprite.transform.localPosition = Vector3.zero;
                _propSprite.transform.rotation = Quaternion.identity;
                _propSprite.transform.localScale = Vector3.one;
                _meshRend.enabled = false;
                _username.SetActive(false);

                _sender.SendSingleData
                (
                    FromClientToServerPackets.BroadcastPropPositionXY,
                    new PropPositionXYFromClientToServerData
                    {
                        PositionXY = new HKMPVector2(_propSprite.transform.localPosition.x, _propSprite.transform.localPosition.y),
                    }
                );

                _sender.SendSingleData
                (
                    FromClientToServerPackets.BroadcastPropPositionZ,
                    new PropPositionZFromClientToServerData
                    {
                        PositionZ = _propSprite.transform.localPosition.z,
                    }
                );

                _sender.SendSingleData
                (
                    FromClientToServerPackets.BroadcastPropRotation,
                    new PropRotationFromClientToServerData
                    {
                        Rotation = _propSprite.transform.rotation.eulerAngles.z,
                    }
                );

                _sender.SendSingleData
                (
                    FromClientToServerPackets.BroadcastPropScale,
                    new PropScaleFromClientToServerData
                    {
                        ScaleFactor = _propSprite.transform.localScale.x,
                    }
                );
            }
            else
            {
                _meshRend.enabled = true;
                _username.SetActive(true);
            }

            _sender.SendSingleData
            (
                FromClientToServerPackets.BroadcastPropSprite,
                new PropSpriteFromClientToServerData
                {
                    SpriteName = spriteName,
                }
            );
        }

        private void ReadMovementInputs()
        {
            switch (_propState)
            {
                case PropState.TranslateXY:
                    var moveXY = _input.inputActions.moveVector.Value * Time.deltaTime * TRANSLATE_XY_SPEED;
                    moveXY = new Vector3(moveXY.x * transform.localScale.x, moveXY.y);
                    Prop.transform.localPosition += (Vector3)moveXY;
                    var clampedVector = Vector2.ClampMagnitude(Prop.transform.localPosition, XY_MAX_MAGNITUDE);
                    var clampedPos = new Vector3(clampedVector.x, clampedVector.y, Prop.transform.localPosition.z);
                    Prop.transform.localPosition = clampedPos;
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
                    clampedPos = new Vector3(Prop.transform.localPosition.x, Prop.transform.localPosition.y, Mathf.Clamp(Prop.transform.localPosition.z, MIN_Z, MAX_Z));
                    Prop.transform.localPosition = clampedPos;
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
                case PropState.Scale:
                    var scaleFactor = Prop.transform.localScale.x +  _input.inputActions.moveVector.Value.y * Time.deltaTime * SCALE_SPEED;
                    scaleFactor = Mathf.Clamp(scaleFactor, MIN_SCALE, MAX_SCALE);
                    Prop.transform.localScale = Vector3.one * scaleFactor;
                    _sender.SendSingleData
                    (
                        FromClientToServerPackets.BroadcastPropScale,
                        new PropScaleFromClientToServerData
                        {
                            ScaleFactor = Prop.transform.localScale.x,
                        }
                    );
                    break;
            }
        }
    }
}