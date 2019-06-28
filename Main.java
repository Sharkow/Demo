import java.io.Console;
import java.lang.reflect.Array;
import java.util.Scanner;

/**
 * Created by IntelliJ IDEA.
 * User: coolermac
 * Date: 13.10.11
 * Time: 20:18
 * To change this template use File | Settings | File Templates.
 */

public class Main {

    static int n, k, totalSeqs;
    static int[] upcogs;//Массив содержит заранее рассчитанное количество возможных "зубьев",
    //направленных вверх, начинающихся от данного числа
    static int[][] seqWeb;

    static int totalEx = 0;

    public static void main(String [] args)
    {
        if (args.length >= 2)
        {
            try
            {
                n = Integer.parseInt(args[0]);
                k = Integer.parseInt(args[1]);
            }
            catch (NumberFormatException e)
            {
                n = k = -1;
                readNKFromConsole();
            }
        }
        else
            readNKFromConsole();

        upcogs = new int[k + 1];
        //countCogs();

        totalSeqs = 0;

        seqWeb = new int[k+1][2];//Нулевой индекс массива просто не будем использовать, для наглядности

        boolean arrowUp = true;
        byte currInd = 1;
        for (int z = 1; z <= k; z++)
            seqWeb[z][1] = 1;
        for (int j = n-1; j >= 1; j--)
        {
            if (arrowUp)
            {
                for (int i = 1; i<=k; i++)
                {
                    seqWeb[i][0] = 0;
                    for (int i1 = i+1; i1 <= k; i1++)
                        seqWeb[i][0] += seqWeb[i1][1];
                }
            }
            else
            {
                  for (int i = 1; i<=k; i++)
                  {
                      seqWeb[i][1] = 0;
                      for (int i1 = i-1; i1 >= 1; i1--)
                          seqWeb[i][1] += seqWeb[i1][0];
                  }
            }
            arrowUp = !arrowUp;
            System.out.println("Посчитано для длины последовательности: " + ++totalEx);
        }

        totalSeqs = 0;
        if (arrowUp) //То читаем из 1 столбца, иначе - из 0
        {
            for (int i = 1; i <= k; i++)
            {
                totalSeqs += seqWeb[i][1];
            }
        }
        else
        {
            for (int i = 1; i <= k; i++)
            {
                totalSeqs += seqWeb[i][0];
            }
        }

        totalSeqs *= 2;

        /*for (int i = 1; i <= k; i++)
        {
            totalSeqs +=countSeqsRecursive(i, 1);
            System.out.println("Посчитано для i = " + i);
        }

         totalSeqs *= 2;*/

        System.out.println(totalSeqs);
    }

    /**
     * Метод вычисляет результат - общее количество последовательностей.
     * Работает рекурсивно - для каждого "зуба" (состоящего из трёх чисел) считает количество возможных следующих "зубов".
     * @param i
     * Число, с которого начинаются искомые "зубья".
     * @param seqPosition
     * Номер этого числа - позиция в последовательности (может быть от 1 до n).
     */
    private static int countSeqsRecursive(int i, int seqPosition)
    {
        if (seqPosition < n - 2)
        {
            int res = 0;
            int nextSeqPosition = seqPosition + 2;
            for (int j = i+1; j < k; j++)
            {
                //System.out.println(++totalEx);
                res += countSeqsRecursive(j, nextSeqPosition) * j;
            }
            return res;
        }
        else
        {
            if (seqPosition == n - 1)
            {
                return k - i;
            }
            else
            {
                return upcogs[i];
            }
        }
    }

    private static void countCogs()
    {
        int inc, summ;
        for (int i = 1; i <= k; i++)
        {
           summ = 0;
            for (inc = i; inc <= k-1; inc++)
                summ += inc;
            upcogs[i] = summ;
        }
    }

    private static void readNKFromConsole()
    {
        String[] strIn;
        Scanner in = new Scanner(System.in);
            do
            {
                System.out.println("Введите числа n и k через пробел, 1 <= n, k <= 1 000 000:");
                strIn = in.nextLine().split(" ");
                if (strIn.length == 2)
                {
                    try
                    {
                        n = Integer.parseInt(strIn[0]);
                        k = Integer.parseInt(strIn[1]);
                    }
                    catch (NumberFormatException e)
                    {}
                }
            }
            while (!(n >= 1 && n <= 1000000 && k >= 1 && k <= 1000000));
    }
}
