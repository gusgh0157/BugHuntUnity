// 랩 49: 던전 길 찾기
// 몬스터가 플레이어 방까지 문을 몇 개 지나야 하는지 너비 우선 탐색(BFS)으로 센다. 평소에는 잘 된다.
// 그런데 플레이어가 문을 잠가 길이 끊기는 순간 게임이 멈추고, 메모리 사용량이 치솟는다.
// 기대: 갈 수 있으면 최소 문 수, 갈 수 없으면 -1. 어느 쪽이든 바로 답한다.
// (HangGuard.Tick 은 지우지 마세요. Core/HangGuard.cs 참고)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BugHunt.Labs.Lab49
{
    public class Dungeon
    {
        // Doors[a] = a 번 방에서 문으로 바로 갈 수 있는 방들. 문은 양방향이다.
        public readonly List<int>[] Doors;

        public Dungeon(int rooms)
        {
            Doors = new List<int>[rooms];
            for (int i = 0; i < rooms; ++i)
            {
                Doors[i] = new List<int>();
            }
        }

        public void Connect(int a, int b)
        {
            Doors[a].Add(b);
            Doors[b].Add(a);
        }

        public void Lock(int a, int b)
        {
            Doors[a].Remove(b);
            Doors[b].Remove(a);
        }
    }

    public static class PathFinder
    {
        // start 에서 goal 까지 지나야 하는 최소 문 수. 갈 수 없으면 -1.
        public static int DoorsBetween(Dungeon dungeon, int start, int goal)
        {
            var queue = new Queue<(int Room, int Doors)>();
            queue.Enqueue((start, 0));
            while (queue.Count > 0)
            {
                // 큐가 메모리를 다 먹기 전에 멈추도록 반복 한도를 작게 둔다.
                HangGuard.Tick("PathFinder.DoorsBetween", 2_000_000);
                var (room, doors) = queue.Dequeue();
                if (room == goal)
                {
                    return doors;
                }
                foreach (int next in dungeon.Doors[room])
                {
                    queue.Enqueue((next, doors + 1));
                }
            }
            return -1;
        }
    }

    [Lab(49, "던전 길 찾기")]
    public class Lab49_Pathfinding : Lab
    {
        public override IEnumerator Run()
        {
            // 방 0~7. 0-1-2-3 복도, 1-4-5-2 우회로(고리), 3-6-7 이 플레이어 쪽 날개. 3-6 문이 그 날개로 가는 유일한 길이다.
            int rooms = 8;
            var dungeon = new Dungeon(rooms);
            dungeon.Connect(0, 1);
            dungeon.Connect(1, 2);
            dungeon.Connect(2, 3);
            dungeon.Connect(1, 4);
            dungeon.Connect(4, 5);
            dungeon.Connect(5, 2);
            dungeon.Connect(3, 6);
            dungeon.Connect(6, 7);
            int monster = Case(0, 4);
            int player = Case(7, 6);
            int expected = Case(5, 4);
            yield return null;

            int doors = PathFinder.DoorsBetween(dungeon, monster, player);
            Report.Expect($"몬스터(방 {monster}) → 플레이어(방 {player}) 문 수", expected, doors);
            yield return null;

            Report.Note("플레이어가 3-6 문을 잠갔다.");
            dungeon.Lock(3, 6);
            Report.Expect("길이 끊긴 뒤 문 수 (갈 수 없음)", -1, PathFinder.DoorsBetween(dungeon, monster, player));
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// while 첫 줄에 조건부 중단점을 거세요. 조건: queue.Count > 100. 방은 8개뿐인데 큐에 100개가 넘게 들어가는 순간이 오나요?
// 멈추면 조사식에 queue 를 넣고 펼쳐 보세요. 같은 방 번호가 몇 번이나 들어 있나요?
// 진짜 게임이었다면 작업 관리자에서 메모리가 계속 늘다가 결국 메모리 부족으로 죽습니다. "멈춤 + 메모리 증가" 는 이 버그의 지문입니다.
#endregion

#region 힌트 2
// 길을 찾았을 때는 왜 멀쩡했을까요? BFS 는 목표를 만나면 바로 return 합니다. 같은 방을 여러 번 넣어도 언젠가는 목표에 닿습니다(느릴 뿐).
// 목표에 닿을 수 없으면? 1 → 2 → 1 → 2 ... 처럼 고리와 양방향 문을 따라 끝없이 되돌아갑니다. 큐는 비지 않습니다.
// 그래프 탐색에는 "이미 넣은 방은 다시 넣지 않는다" 는 기록이 반드시 있어야 합니다.
#endregion

#region 힌트 3
// 방문 기록을 두고, 큐에 넣을 때 표시하세요:
//   var visited = new bool[dungeon.Doors.Length];  visited[start] = true;
//   foreach (int next in dungeon.Doors[room]) { if (!visited[next]) { visited[next] = true; queue.Enqueue((next, doors + 1)); } }
// (꺼낼 때 표시해도 끝나기는 하지만, 같은 방이 큐에 여러 번 들어가 느려집니다.) 재귀 DFS 에서 같은 실수를 하면 멈춤 대신 StackOverflow 로 죽습니다.
#endregion
