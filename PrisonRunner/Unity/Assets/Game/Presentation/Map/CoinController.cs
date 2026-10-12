using Muhanok.Domain;
using UnityEngine;

namespace Muhanok.Presentation.Map
{
    public sealed class CoinController : MonoBehaviour
    {
        private bool collected;
        public void ResetCoin() { collected=false; transform.localRotation=Quaternion.identity; }
        public void Evaluate(RunSession run,float previousZ)
        {
            if(collected||!gameObject.activeSelf||run.Flow.State!=GameFlowState.Running) return;
            if(previousZ>transform.position.z||run.Runner.Z<transform.position.z) return;
            RouteSurface.Sample(run.Runner.Z,out float x,out float y);
            // The magnet item reaches the neighbouring lanes and any jump height.
            bool magnet=run.Items.IsActive(ItemKind.Magnet);
            if(Mathf.Abs(run.Runner.WorldLaneOffset+x-transform.position.x)>(magnet?3.4f:.8f)||Mathf.Abs(run.Runner.Y+y+.85f-transform.position.y)>(magnet?3f:.9f)) return;
            collected=true; run.Score.CollectCoin(); gameObject.SetActive(false);
        }
        private void Update() { transform.Rotate(0,Time.deltaTime*90,0,Space.Self); }
    }
}
