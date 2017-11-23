using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameForLearningFeature
{
    class Program
    {
        private static int size = 10;
        private static int[,] array;
        private static List<Block> blocks;
        private static Player player;
        private static Dictionary<uint, Dictionary<uint, int>> qTable;
        private static uint preState;
        private static uint preAction;
        private static float epsilon = 0.0f;
        private static float learningRate = 1f;
        private static float gamma = 0.8f;
        private static int bombCount = 0;
        private static int coinCount = 0;
        private static Random rand = new Random();
        private static uint learnCount = 0;
        private static string memoryFile = "\\AI.txt";
        private static FileStream fs = null;
        private static StreamWriter writer = null;
        private static StreamReader reader = null;
        private static bool exit = false;
        public static char coin = '@';
        public static char bomb = '*';
        public static float[] weights;
        private static int fCount = 15;
        private static float stepSize = 0.001f;
        static void Main(string[] args)
        {

            string filePath = Directory.GetCurrentDirectory() + memoryFile;
            qTable = new Dictionary<uint, Dictionary<uint, int>>();
            weights = new float[fCount];
            if (File.Exists(filePath))
            {
                fs = File.Open(filePath, FileMode.Open);
                reader = new StreamReader(fs);
                string strLine = reader.ReadLine();
                int i = 0;
                while (strLine != null && strLine !="" && i<fCount)
                {
                    //uint state, action;
                    // int reward;
                    //Decode(strLine, out state, out action, out reward);
                    weights[i] = float.Parse(strLine);
                    i++;
                    strLine = reader.ReadLine();
                }

                reader.Close();

                fs.Close();
            }


            Thread thread = new Thread(Listener);
            thread.Name = "Listener";
            thread.Start();

            array = new int[size, size];
            player = new Player();
            blocks = new List<Block>();
            blocks.Add(new Block());
            preState = 0;
            preAction = 0;
            while (true)
            {

                if (epsilon == 0)
                    Thread.Sleep(300);
                Update();
                if (exit)
                    break;

            }
            if(epsilon!=0)
            Save();


        }
        static void Save()
        {
            string filePath = Directory.GetCurrentDirectory() + memoryFile;

            if (!File.Exists(filePath))
            {
                fs = File.Create(filePath);
            }
            else
            {
                fs = File.Open(filePath, FileMode.Open);
            }
            writer = new StreamWriter(fs);
            for (int i = 0; i < fCount; i++)
            {
                writer.WriteLine(weights[i]);
            }

            writer.Flush();
            writer.Close();
            fs.Close();
        }

        static void Episode(float q)
        {

            float reward = Reward(GetStateOfGame());
            ActionType action;
            if (rand.NextDouble() < epsilon)
                action = ChooseRandomAction();
            else
                action = ChooseBestAction();



            //float delta = reward + gamma * GetQValue(blocks, (uint)action) - q;

            //UpdateWeights(delta);
            preAction = (uint)action;
            learnCount++;

        }
        static void SetPlayerAction(ActionType action)
        {
            preAction = (uint)action;
            if (action == ActionType.Left)
                player.y--;
            if (action == ActionType.Right)
                player.y++;
        }
        static ActionType ChooseRandomAction()
        {
            int action = rand.Next() % 3;
            if (player.y == 0)
            {
                action = (int)ActionType.Stay;
                if (rand.NextDouble() < 0.5) return ActionType.Right;
            }
            if (player.y == size - 1)
            {
                action = (int)ActionType.Stay;
                if (rand.NextDouble() < 0.5) return ActionType.Left;
            }

            return (ActionType)action;

        }
        /*At current blocks is our game state.*/
        static float GetQValue(List<Block> blocks, uint action)
        {
            float value = 0;
            for (int i = 0; i < fCount; i++)
            {
                value += weights[i] * GetFeatureValue(i);
            }

            return value;
        }
        static float GetQValuePredict(List<Block> blocks,uint action)
        {
            float value = 0;
            for (int i = 0; i < fCount; i++)
            {
                value += weights[i] * GetFeatureValuePredict(blocks,action,i);
            }

            return value;
        }
        static ActionType ChooseBestAction()
        {
            ActionType action = ActionType.Stay;
            float value = -100000;
            for (uint i = 0; i < 3; i++)
            {
                if (player.y == 0 && i == 1) continue;
                if (player.y == size - 1 && i == 2) continue; 
                if (GetQValuePredict(blocks, i) > value)
                {
                    value = GetQValuePredict(blocks, i);
                    action = (ActionType)i;
                }
            }

            return action;
        }
        static void UpdateEnvironment()
        {
            Block blockA = new Block();
            Block blockB = new Block();
            blocks.Add(blockA);
            if (blockB.y != blockA.y)
                blocks.Add(blockB);

            for (int i = 0; i < blocks.Count; i++)
            {
                blocks[i].update();
                if (blocks[i].x >= size)
                {
                    blocks.RemoveAt(i);
                    i--;
                    continue;
                }

                array[blocks[i].x, blocks[i].y] = blocks[i].value;
            }
            if (preAction == (uint)ActionType.Left) player.y--;
            if (preAction == (uint)ActionType.Right) player.y++;
            SetPlayerPosition();
        }
        static void ShowScore()
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                if (player.x == blocks[i].x && player.y == blocks[i].y && blocks[i].value == bomb)
                {
                    ++bombCount;
                    break;
                }
                if (player.x == blocks[i].x && player.y == blocks[i].y && blocks[i].value == coin)
                {
                    ++coinCount;
                    break;
                }

            }
            Console.WriteLine("Bomb hit：{0}", bombCount);
            Console.WriteLine("Coin hit: {0}", coinCount);
            Console.WriteLine("Iteration: {0}", learnCount);


        }
        static void Update()
        {


            float q = GetQValue(blocks, preAction);
            
            ClearGamePanel();
            UpdateEnvironment();
            PrintGamePanel();
            ShowScore();
            Episode(q);
           

        }
        static void Listener()
        {
            while (true)
            {
                ConsoleKey InputKey = Console.ReadKey().Key;
                switch (InputKey)
                {
                    case ConsoleKey.LeftArrow:
                        if (Program.player.y > 0)
                            Program.player.y--;
                        break;
                    case ConsoleKey.RightArrow:
                        if (Program.player.y < size - 1)
                            Program.player.y++;
                        break;
                    case ConsoleKey.UpArrow:
                        exit = true;
                        return;


                }

            }


        }


        static void UpdateWeights(float delta)
        {
            float[] fs = new float[fCount];
            for (int i = 0; i < fCount; i++)
            {
                fs[i] = GetFeatureValue(i);
            }
            for (int i = 0; i < fCount; i++)
            {
                weights[i] = weights[i] + stepSize * delta * fs[i];
            }
            stepSize *= 0.98f;
        }


        static int GetFeatureValuePredict(List<Block> ablocks, uint action, int featureIndex)
        {
            List<Block> blockss = new List<Block>();


            for (int i = 0; i < ablocks.Count; i++)
            {
                blockss.Add(new Block(blocks[i]));
            }
            for (int i = 0; i < blockss.Count; i++)
            {
                blockss[i].update();
            }
            Player tmp = new Player(player.x, player.y);
            if (action == (uint)ActionType.Left) tmp.y--;
            if (action == (uint)ActionType.Right) tmp.y++;
            uint state = GetStateOfGame(blockss, tmp);

            //featureIndex += 2;// skip hit bomb and get coin.
            bool bomb = (state & (1 << featureIndex)) != 0;
            bool coin = (state & (1 << featureIndex + fCount)) != 0;
            if (featureIndex == 0)
            {
                if (bomb)
                    return -1;
                else
                    return 0;
            }
            if (featureIndex == fCount - 1)
            {
                bool coins = (state & (1 << (fCount -1))) != 0;
                if (coins)
                    return 1;
                else
                    return 0;
            }

            if (coin)
                return 1;
            if (bomb)
                return -1;
            return 0;
        }
        
        static int GetFeatureValue(int featureIndex)
        {
            //featureIndex += 2;// skip hit bomb and get coin.

            uint state = GetStateOfGame();
            bool bomb = (state & (1 << featureIndex)) != 0;
            bool coin = (state & (1 << featureIndex + fCount)) != 0;
            if (featureIndex == 0)
            {
                if (bomb)
                    return -1;
                else
                    return 0;
            }
            if (featureIndex == fCount - 1)
            {
                bool coins = (state & (1 << (fCount-1)) ) != 0;
                if (coins)
                    return 1;
                else
                    return 0;
            }
            if (coin)
                return 1;
             if(bomb)
                return -1 ;
            return 0;
            
        }
        static uint GetStateOfGame(List<Block> sblocks, Player player)
        {
            uint state = 0;
            for (int i = 0; i < sblocks.Count; i++)
            {
                if (player.x == sblocks[i].x && player.y == sblocks[i].y)
                {
                    if (sblocks[i].value == bomb)
                        state |= (int)StateSubType.HitEnemy;
                    else
                        state |= (int)StateSubType.HitFood;
                }

                if (player.y == sblocks[i].y)
                {

                    if (player.x - 1 == sblocks[i].x)
                    {
                        if (sblocks[i].value == bomb)
                            state |= (uint)StateSubType.FaceBottomEnemy;
                        else
                            state |= (uint)StateSubType.FaceBottomFood;
                    }
                    if (player.x - 2 == sblocks[i].x)
                    {
                        if (sblocks[i].value == bomb)
                            state |= (uint)StateSubType.FaceMiddleEnemy;
                        else
                            state |= (uint)StateSubType.FaceMiddleFood;
                    }
                    if (player.x - 3 == sblocks[i].x)
                    {
                        if (sblocks[i].value == bomb)
                            state |= (uint)StateSubType.FaceTopEnemy;
                        else
                            state |= (uint)StateSubType.FaceTopFood;
                    }
                }

                if (player.y - 1 == sblocks[i].y)
                {
                    if (player.x - 1 == sblocks[i].x)
                    {
                        if (sblocks[i].value == bomb)
                            state |= (uint)StateSubType.LeftBottomEnemy;
                        else
                            state |= (uint)StateSubType.LeftBottomFood;
                    }
                    if (player.x - 2 == sblocks[i].x)
                    {
                        if (sblocks[i].value == bomb)
                            state |= (uint)StateSubType.LeftMiddleEnemy;
                        else
                            state |= (uint)StateSubType.LeftMiddleFood;
                    }
                    if (player.x - 3 == sblocks[i].x)
                    {
                        if (sblocks[i].value == bomb)
                            state |= (uint)StateSubType.LeftTopEnemy;
                        else
                            state |= (uint)StateSubType.LeftTopFood;
                    }
                }
                if (player.y + 1 == sblocks[i].y)
                {
                    if (player.x - 1 == sblocks[i].x)
                    {
                        if (sblocks[i].value == bomb)
                            state |= (uint)StateSubType.RightBottomEnemy;
                        else
                            state |= (uint)StateSubType.RightBottomFood;
                    }
                    if (player.x - 2 == sblocks[i].x)
                    {
                        if (sblocks[i].value == bomb)
                            state |= (uint)StateSubType.RightMiddleEnemy;
                        else
                            state |= (uint)StateSubType.RightMiddleFood;
                    }
                    if (player.x - 3 == sblocks[i].x)
                    {
                        if (sblocks[i].value == bomb)
                            state |= (uint)StateSubType.RightTopEnemy;
                        else
                            state |= (uint)StateSubType.RightTopFood;
                    }
                }
                if (player.y - 2 == sblocks[i].y)
                {
                    if (player.x - 2 == sblocks[i].x)
                    {
                        if (sblocks[i].value == bomb)
                            state |= (uint)StateSubType.LeftLeftMiddleEnemy;
                        else
                            state |= (uint)StateSubType.LeftLeftMiddleFood;
                    }
                    if (player.x - 3 == sblocks[i].x)
                    {
                        if (sblocks[i].value == bomb)
                            state |= (uint)StateSubType.LeftLeftTopEnemy;
                        else
                            state |= (uint)StateSubType.LeftLeftTopFood;
                    }
                }
                if (player.y + 2 == sblocks[i].y)
                {
                    if (player.x - 2 == sblocks[i].x)
                    {
                        if (sblocks[i].value == bomb)
                            state |= (uint)StateSubType.RightRightMiddleEnemy;
                        else
                            state |= (uint)StateSubType.RightRightMiddleFood;
                    }
                    if (player.x - 3 == sblocks[i].x)
                    {
                        if (sblocks[i].value == bomb)
                            state |= (uint)StateSubType.RightRightTopEnemy;
                        else
                            state |= (uint)StateSubType.RightRightTopFood;
                    }
                }

            }

            if (player.y == 0)
            {

                state |= (uint)StateSubType.LeftBottomEnemy;

                state |= (uint)StateSubType.LeftMiddleEnemy;

                state |= (uint)StateSubType.LeftTopEnemy;

            }
            if (player.y == size - 1)
            {

                state |= (uint)StateSubType.RightBottomEnemy;

                state |= (uint)StateSubType.RightMiddleEnemy;

                state |= (uint)StateSubType.RightTopEnemy;

            }


            return state;
        }
        static uint GetStateOfGame()
        {
            uint state = 0;
            for (int i = 0; i < blocks.Count; i++)
            {
                if (player.x == blocks[i].x && player.y == blocks[i].y)
                {
                    if (blocks[i].value == bomb)
                        state |= (int)StateSubType.HitEnemy;
                    else
                        state |= (int)StateSubType.HitFood;
                }

                if (player.y == blocks[i].y)
                {

                    if (player.x - 1 == blocks[i].x)
                    {
                        if (blocks[i].value == bomb)
                            state |= (uint)StateSubType.FaceBottomEnemy;
                        else
                            state |= (uint)StateSubType.FaceBottomFood;
                    }
                    if (player.x - 2 == blocks[i].x)
                    {
                        if (blocks[i].value == bomb)
                            state |= (uint)StateSubType.FaceMiddleEnemy;
                        else
                            state |= (uint)StateSubType.FaceMiddleFood;
                    }
                    if (player.x - 3 == blocks[i].x)
                    {
                        if (blocks[i].value == bomb)
                            state |= (uint)StateSubType.FaceTopEnemy;
                        else
                            state |= (uint)StateSubType.FaceTopFood;
                    }
                }

                if (player.y - 1 == blocks[i].y)
                {
                    if (player.x - 1 == blocks[i].x)
                    {
                        if (blocks[i].value == bomb)
                            state |= (uint)StateSubType.LeftBottomEnemy;
                        else
                            state |= (uint)StateSubType.LeftBottomFood;
                    }
                    if (player.x - 2 == blocks[i].x)
                    {
                        if (blocks[i].value == bomb)
                            state |= (uint)StateSubType.LeftMiddleEnemy;
                        else
                            state |= (uint)StateSubType.LeftMiddleFood;
                    }
                    if (player.x - 3 == blocks[i].x)
                    {
                        if (blocks[i].value == bomb)
                            state |= (uint)StateSubType.LeftTopEnemy;
                        else
                            state |= (uint)StateSubType.LeftTopFood;
                    }
                }
                if (player.y + 1 == blocks[i].y)
                {
                    if (player.x - 1 == blocks[i].x)
                    {
                        if (blocks[i].value == bomb)
                            state |= (uint)StateSubType.RightBottomEnemy;
                        else
                            state |= (uint)StateSubType.RightBottomFood;
                    }
                    if (player.x - 2 == blocks[i].x)
                    {
                        if (blocks[i].value == bomb)
                            state |= (uint)StateSubType.RightMiddleEnemy;
                        else
                            state |= (uint)StateSubType.RightMiddleFood;
                    }
                    if (player.x - 3 == blocks[i].x)
                    {
                        if (blocks[i].value == bomb)
                            state |= (uint)StateSubType.RightTopEnemy;
                        else
                            state |= (uint)StateSubType.RightTopFood;
                    }
                }
                if (player.y - 2 == blocks[i].y)
                {
                    if (player.x - 2 == blocks[i].x)
                    {
                        if (blocks[i].value == bomb)
                            state |= (uint)StateSubType.LeftLeftMiddleEnemy;
                        else
                            state |= (uint)StateSubType.LeftLeftMiddleFood;
                    }
                    if (player.x - 3 == blocks[i].x)
                    {
                        if (blocks[i].value == bomb)
                            state |= (uint)StateSubType.LeftLeftTopEnemy;
                        else
                            state |= (uint)StateSubType.LeftLeftTopFood;
                    }
                }
                if (player.y + 2 == blocks[i].y)
                {
                    if (player.x - 2 == blocks[i].x)
                    {
                        if (blocks[i].value == bomb)
                            state |= (uint)StateSubType.RightRightMiddleEnemy;
                        else
                            state |= (uint)StateSubType.RightRightMiddleFood;
                    }
                    if (player.x - 3 == blocks[i].x)
                    {
                        if (blocks[i].value == bomb)
                            state |= (uint)StateSubType.RightRightTopEnemy;
                        else
                            state |= (uint)StateSubType.RightRightTopFood;
                    }
                }

            }

            if (player.y == 0)
            {

                state |= (uint)StateSubType.LeftBottomEnemy;

                state |= (uint)StateSubType.LeftMiddleEnemy;

                state |= (uint)StateSubType.LeftTopEnemy;

            }
            if (player.y == size - 1)
            {

                state |= (uint)StateSubType.RightBottomEnemy;

                state |= (uint)StateSubType.RightMiddleEnemy;

                state |= (uint)StateSubType.RightTopEnemy;

            }


            return state;
        }
        static int Reward(uint curState)
        {


            if ((curState & (uint)StateSubType.HitEnemy) != 0)
                return -10;
            if ((curState & (uint)StateSubType.HitFood) != 0)
                return +1;


            return 0;
        }
        static void SetPlayerPosition()
        {
            array[player.x, player.y] = 'A';
        }
        static void PrintGamePanel()
        {
          
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    Console.Write(" ");
                    if (array[i, j] == 0)
                        Console.Write(" ");
                    else
                        Console.Write((char)array[i, j]);

                }
                Console.WriteLine();
            }
            //Console.WriteLine(Reward(GetStateOfGame()));

        }
        static void ClearGamePanel()
        {

            Console.SetCursorPosition(0, 0);
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    array[i, j] = 0;
                }

            }
        }
    }
    class Block
    {
        public int x, y;
        public int value;
        static Random rand = new Random();
        public Block()
        {

            y = rand.Next() % 10;
            x = 0;
            value = rand.Next() % 2 > 0 ? Program.coin : Program.bomb;
        }
        public Block(Block block)
        {
            x = block.x;
            y = block.y;
            value = block.value;
        }
        public void update()
        {
            x++;
        }
    }
    class Player
    {
        public int x, y;
        public Player()
        {
            x = 9;
            y = 4;
        }
        public Player(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

    }
    enum StateSubType : int
    {
        HitEnemy = 1,
        LeftTopEnemy = 1 << 1,
        LeftMiddleEnemy = 1 << 2,
        LeftBottomEnemy = 1 << 3,
        FaceTopEnemy = 1 << 4,
        FaceMiddleEnemy = 1 << 5,
        FaceBottomEnemy = 1 << 6,
        RightTopEnemy = 1 << 7,
        RightMiddleEnemy = 1 << 8,
        RightBottomEnemy = 1 << 9,
        LeftLeftTopEnemy = 1 << 10,
        LeftLeftMiddleEnemy = 1 << 11,
        RightRightTopEnemy = 1 << 12,
        RightRightMiddleEnemy = 1 << 13,
        HitFood = 1 << 14,
        LeftTopFood = 1 << 15,
        LeftMiddleFood = 1 << 16,
        LeftBottomFood = 1 << 17,
        FaceTopFood = 1 << 18,
        FaceMiddleFood = 1 << 19,
        FaceBottomFood = 1 << 20,
        RightTopFood = 1 << 21,
        RightMiddleFood = 1 << 22,
        RightBottomFood = 1 << 23,
        LeftLeftTopFood = 1 << 24,
        LeftLeftMiddleFood = 1 << 25,
        RightRightTopFood = 1 << 26,
        RightRightMiddleFood = 1 << 27,


    }
    enum ActionType : int
    {
        Stay,
        Left,
        Right
    }

    
}
