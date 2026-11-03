using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

namespace BugHunt
{
    // HangGuard 가 "응답 없음"을 선언하며 던지는 예외. LabRunner 는 이것을 크래시가 아니라 TIMEOUT 으로 센다.
    public sealed class HangException : Exception
    {
        public HangException(string message) : base(message) { }
    }

    // 7부(멈춤) 랩의 안전장치. 여기에는 버그가 없다.
    //
    // 진짜 무한루프나 데드락은 메인 스레드를 영영 붙잡아서 에디터 전체가 굳는다(작업 관리자로 끄는 수밖에 없다).
    // 그래서 랩의 반복문 안에 HangGuard.Tick() 한 줄을, 막히는 대기에는 HangGuard.Wait(task) 를 넣어 두었다.
    //   - Tick 은 아무것도 바꾸지 않고 아무것도 돌려주지 않는다. 반복문의 조건과 몸통은 그대로다.
    //     그래서 버그를 고쳐 반복문이 정상적으로 끝나면 Tick 은 있으나 마나다(가리는 것이 없다).
    //   - 한 프레임 안에서 시간 예산(기본 2초)이나 반복 한도(정했다면)를 넘으면 [FAIL] 응답 없음 을 찍고 HangException 을 던져 빠져나온다.
    //     예외의 스택 트레이스 = 메인 스레드가 갇혀 있던 곳. 디버거의 "모두 중단" 으로 보는 호출 스택과 같다.
    //   - Wait 는 task.Result 와 같지만, 예산 안에 안 끝나면 데드락으로 보고 빠져나온다.
    //
    // ⚠ Tick / Wait 줄을 지우지 마세요. 지운 채로 버그가 남아 있으면 에디터가 정말로 굳습니다.
    // 디버거를 붙여 "모두 중단" 을 눌러 보고 싶으면 메뉴 BugHunt → 멈춤 랩: 오래 버티기(30초) 를 켜세요.
    public static class HangGuard
    {
        public const float DefaultBudgetSeconds = 2f;

        // 이보다 긴 Tick 사이 간격은 "디버거 중단점에 멈춰 있었다"로 보고 시간에 넣지 않는다.
        const double PauseGapSeconds = 0.25;

        public static float BudgetSeconds { get; set; } = DefaultBudgetSeconds;

        public static bool Tripped { get; private set; }
        public static string TrippedWhat { get; private set; }

        static readonly Stopwatch clock = Stopwatch.StartNew();
        static int tickFrame = -1;
        static long iterations;
        static double stuckSeconds;
        static double lastTick;

        // LabRunner 가 랩마다 부른다.
        public static void Reset()
        {
            Tripped = false;
            TrippedWhat = null;
            tickFrame = -1;
        }

        // 반복문 안에 한 줄. 같은 프레임 안에서 시간 예산을 넘기면 빠져나온다. 프레임이 넘어가면(yield) 다시 0 부터 잰다.
        // maxIterations: 돌 때마다 메모리를 먹는 반복문(큐에 계속 넣는 탐색 등)은 시간보다 반복 수로 먼저 끊는다.
        public static void Tick(string what, long maxIterations = long.MaxValue)
        {
            if (Tripped)
            {
                throw new HangException($"응답 없음 (이미 선언됨): {what}");
            }
            double now = clock.Elapsed.TotalSeconds;
            int frame = Time.frameCount;
            if (frame != tickFrame)
            {
                tickFrame = frame;
                iterations = 0;
                stuckSeconds = 0;
                lastTick = now;
                return;
            }
            double gap = now - lastTick;
            lastTick = now;
            if (gap < PauseGapSeconds)
            {
                stuckSeconds += gap;
            }
            ++iterations;
            if (stuckSeconds > BudgetSeconds)
            {
                Trip(what, $"{BudgetSeconds:0.#}초 동안 한 프레임에서 빠져나오지 못함, 반복 {iterations:N0}번");
            }
            if (iterations > maxIterations)
            {
                Trip(what, $"한 프레임에 반복 {iterations:N0}번 (한도 {maxIterations:N0}), {stuckSeconds:0.00}초");
            }
        }

        // task.Result 와 같다. 단, 예산 안에 끝나지 않으면 데드락으로 보고 빠져나온다.
        public static T Wait<T>(Task<T> task, string what)
        {
            if (!task.Wait(TimeSpan.FromSeconds(BudgetSeconds)))
            {
                Trip(what, $"{BudgetSeconds:0.#}초를 기다려도 Task 가 끝나지 않음 (상태: {task.Status})");
            }
            return task.Result;
        }

        static void Trip(string what, string why)
        {
            Tripped = true;
            TrippedWhat = what;
            Report.Fail($"응답 없음: {what} ({why}). 진짜 게임이었다면 여기서 에디터가 굳었습니다");
            throw new HangException($"응답 없음: {what}. 아래 스택 트레이스가 메인 스레드가 갇혀 있던 곳입니다");
        }
    }
}
