using Muhanok.Domain;
using UnityEngine;

namespace Muhanok.Presentation.Map
{
    public sealed class MuhanokMapChunk : MonoBehaviour
    {
        public const float Length = 24f;
        public string AssetId;
        public int RouteType;
        public Transform Entry, Exit;
        public ObstacleController[] Obstacles;
        public ObstacleController[] ExerciseObstacles = new ObstacleController[0];
        public CoinController[] Coins;
        public DescendingGate[] Gates;
        public FloorItemController[] FloorItems;
        public CartRailGap[] RailGaps;
        public ClosingSlideGate[] SlideGates;
        // Runtime additions placed by the decorator; not part of the authored prefab.
        public readonly System.Collections.Generic.List<ObstacleController> DynamicObstacles = new System.Collections.Generic.List<ObstacleController>();
        public readonly System.Collections.Generic.List<ItemPickup> Pickups = new System.Collections.Generic.List<ItemPickup>();
        public readonly System.Collections.Generic.List<IRunnerAnimated> Animated = new System.Collections.Generic.List<IRunnerAnimated>();
        public void Prepare(int sequence, float chance, System.Random random)
        {
            if(Gates!=null) foreach(var gate in Gates) gate.ResetGate();
            if(SlideGates!=null)foreach(var gate in SlideGates)gate.ResetGate();
            if(FloorItems!=null)foreach(var item in FloorItems){item.ResetItem();item.gameObject.SetActive(sequence>0);}
            if(RailGaps!=null)foreach(var gap in RailGaps){gap.ResetGap();gap.gameObject.SetActive(true);}
            for (int i = 0; i < Obstacles.Length; i++) { Obstacles[i].gameObject.SetActive(false); Obstacles[i].ResetPass();Obstacles[i].FullExerciseRow=false; }
            if (Coins != null) foreach(var coin in Coins) { coin.ResetCoin(); coin.gameObject.SetActive(sequence>0); }
            bool cart=RouteType==1||RouteType==2;
            if(ExerciseObstacles!=null)foreach(var obstacle in ExerciseObstacles)
            {
                obstacle.ResetPass();obstacle.FullExerciseRow=true;
                // The first run opens with a clear warm-up; later mine visits include the jump row.
                bool warmup=sequence==0&&obstacle.Kind==ObstacleKind.Crate;
                obstacle.gameObject.SetActive(!cart&&!warmup);
            }
            if(cart)
            {
                if(Coins!=null)foreach(var coin in Coins){var p=coin.transform.localPosition;p.x=RouteSurface.LocalX(RouteType,p.z);coin.transform.localPosition=p;}
                return;
            }
            for (int row = 0; row < 2; row++)
            {
                // Authored exercise rows supply jump/slide; keep stairs and lane-blocking doors separately.
                if(ExerciseObstacles!=null&&ExerciseObstacles.Length>0)
                {
                    if(RouteType==7)
                    {
                        for(int lane=0;lane<3;lane++){Obstacles[row*3+lane].FullExerciseRow=true;Obstacles[row*3+lane].gameObject.SetActive(true);}
                    }
                    else if(RouteType>=4&&RouteType<=6&&row==0)
                        Obstacles[random.Next(3)].gameObject.SetActive(true);
                    continue;
                }
                if(sequence==0&&row==0)continue;
                bool exercise=RouteType==3||RouteType==7||(RouteType==0&&row==1)||(RouteType==6&&row==1);
                if(exercise)
                {
                    var kind=Obstacles[row*3].Kind;
                    if(!SafePatternValidator.IsActionRowSafe(7,kind))throw new System.InvalidOperationException("Unsafe full-width exercise row");
                    for(int lane=0;lane<3;lane++){Obstacles[row*3+lane].FullExerciseRow=true;Obstacles[row*3+lane].gameObject.SetActive(true);}
                    continue;
                }
                if (random.NextDouble() > chance && !(Gates!=null&&Gates.Length>0&&row==0) && !((RouteType==1||RouteType==2)&&row==0)) continue;
                int laneIndex = random.Next(3);
                // One hazard per row leaves two clear lanes.
                if (!SafePatternValidator.IsSafe(1 << laneIndex)) continue;
                Obstacles[row * 3 + laneIndex].gameObject.SetActive(true);
            }
            if (Coins != null) for(int i=0;i<Coins.Length;i++)
            {
                int row = i/3, lane = (sequence+row)%3;
                if (Obstacles[row*3+lane].gameObject.activeSelf) lane=(lane+1)%3;
                var position=Coins[i].transform.localPosition; position.x=(lane-1)*2.2f+RouteSurface.LocalX(RouteType,position.z); Coins[i].transform.localPosition=position;
            }
        }
        public void AnimateGates(float runnerZ)
        {
            if(Gates!=null) foreach(var gate in Gates) if(gate.gameObject.activeInHierarchy) gate.Preview(runnerZ);
            if(SlideGates!=null)foreach(var gate in SlideGates)if(gate.gameObject.activeInHierarchy)gate.Preview(runnerZ);
            foreach(var item in Animated)item.Preview(runnerZ);
        }
    }
}
