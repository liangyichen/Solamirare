namespace Solamirare.Test
{

    using Solamirare;

    public class CircularDequeTests
    {
        public static bool TestPushFrontAndPopBack()
        {
            CircularDeque<int> deque = new CircularDeque<int>(4);
            deque.PushFront(1);
            deque.PushFront(2);
            deque.PushFront(3);

            if (deque.PopBack() != 1) return false;
            if (deque.PopBack() != 2) return false;
            if (deque.PopBack() != 3) return false;

            deque.Dispose();
            return true;
        }

        public static bool TestPushBackAndPopFront()
        {
            CircularDeque<int> deque = new CircularDeque<int>(4);
            deque.PushBack(1);
            deque.PushBack(2);
            deque.PushBack(3);

            if (deque.PopFront() != 1) return false;
            if (deque.PopFront() != 2) return false;
            if (deque.PopFront() != 3) return false;

            deque.Dispose();
            return true;
        }

        public static bool TestCapacityExpansion()
        {
            CircularDeque<int> deque = new CircularDeque<int>(2);
            deque.PushBack(1);
            deque.PushBack(2);
            deque.PushBack(3);
            deque.PushBack(4);

            if (deque.Capacity != 4) return false;
            if (deque.PopFront() != 1) return false;
            if (deque.PopFront() != 2) return false;
            if (deque.PopFront() != 3) return false;
            if (deque.PopFront() != 4) return false;

            deque.Dispose();
            return true;
        }

        public static bool TestClear()
        {
            CircularDeque<int> deque = new CircularDeque<int>(4);
            deque.PushBack(1);
            deque.PushBack(2);
            deque.PushBack(3);

            deque.Clear();

            if (deque.Count != 0) return false;

            deque.Dispose();
            return true;
        }

        public static bool TestWrapAround()
        {
            CircularDeque<int> deque = new CircularDeque<int>(4);
            deque.PushBack(1);
            deque.PushBack(2);
            deque.PushBack(3);
            deque.PopFront();
            deque.PopFront();
            deque.PushBack(4);
            deque.PushBack(5);

            if (deque.PopFront() != 3) return false;
            if (deque.PopFront() != 4) return false;
            if (deque.PopFront() != 5) return false;

            deque.Dispose();
            return true;
        }


    
    

        public static bool TestFrontAndBack()
        {
            CircularDeque<int> deque = new CircularDeque<int>(4);
            deque.PushBack(1);
            deque.PushBack(2);

            if (deque.PopFront() != 1) return false;
            if (deque.PopBack() != 2) return false;

            deque.PushFront(3);

            if (deque.PopFront() != 3) return false;
            if (deque.PopBack() != 2) return false;

            deque.Dispose();
            return true;
        }

        public static bool TestTrimExcess()
        {
            CircularDeque<int> deque = new CircularDeque<int>(16);
            for (int i = 0; i < 4; i++)
            {
                deque.PushBack(i);
            }

            deque.TrimExcess();

            // Capacity may or may not change, but it should not be less than the current count.
            if (deque.Capacity < deque.Count) return false;

            for (int i = 0; i < 4; i++)
            {
                if (deque.PopFront() != i) return false;
            }

            deque.Dispose();
            return true;
        }

    
    }
}