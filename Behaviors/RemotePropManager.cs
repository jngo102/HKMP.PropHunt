using UnityEngine;

namespace PropHunt.Behaviors
{
    [RequireComponent(typeof(MeshRenderer))]
    internal class RemotePropManager : MonoBehaviour
    {
        private MeshRenderer _meshRend;
        private GameObject _username;
        private SpriteRenderer _propSprite;
        public GameObject Prop { get; private set; }
        public Sprite PropSprite => _propSprite.sprite;

        private void Awake()
        {
            _meshRend = GetComponent<MeshRenderer>();
            _username = transform.parent.Find("Username").gameObject;
            Prop = new GameObject("Prop");
            Prop.transform.SetParent(transform);
            ResetPropTransform();
            _propSprite = Prop.AddComponent<SpriteRenderer>();
        }
        
        /// <summary>
        /// Set the remote player's prop's sprite.
        /// </summary>
        /// <param name="sprite">The sprite to change to</param>
        public void SetPropSprite(Sprite sprite)
        {
            if (_propSprite == null) return;

            _propSprite.sprite = sprite;

            if (sprite == null)
            {
                PropHunt.Instance.Log("Null sprite, showing player.");
                ResetPropTransform();
                _meshRend.enabled = true;
                _username.SetActive(true);
                return;
            }

            PropHunt.Instance.Log("Non null sprite, hiding player.");
            _meshRend.enabled = false;
            if (HeroController.instance.GetComponent<LocalPropManager>()?.PropSprite != null)
            {
                _username.SetActive(false);
            }
        }

        /// <summary>
        /// Reset the prop's transform to default.
        /// </summary>
        private void ResetPropTransform()
        {
            PropHunt.Instance.Log("Resetting prop transform");
            Prop.transform.localPosition = Vector3.zero;
            Prop.transform.rotation = Quaternion.identity;
            Prop.transform.localScale = Vector3.one;
        }
    }
}