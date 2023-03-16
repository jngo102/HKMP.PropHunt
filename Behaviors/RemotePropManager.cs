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

        private void Awake()
        {
            _meshRend = GetComponent<MeshRenderer>();
            _username = transform.parent.Find("Username").gameObject;
            Prop = new GameObject("Prop");
            Prop.transform.SetParent(transform);
            ResetPropTransform();
            _propSprite = Prop.AddComponent<SpriteRenderer>();
        }

        private void OnDisable() => _propSprite.sprite = null;
        
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
                ResetPropTransform();
                _meshRend.enabled = true;
                _username.SetActive(true);
                return;
            }
            
            _meshRend.enabled = false;
            if (HeroController.instance.GetComponent<Hunter>().enabled)
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