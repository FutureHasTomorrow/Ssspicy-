using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Point = System.ValueTuple<int, int>;

public class Map : MonoBehaviour
{
    public Sprite[] grounds;
    public Sprite[] moveheads;
    public Sprite[] stunheads;
    public Sprite[] tearheads;
    public Sprite[] sweatheads;
    public Sprite[] cryheads;
    public Sprite[] smileheads;
    public Sprite[] exitings;
    public Sprite[] straights;
    public Sprite[] turns;
    public Sprite[] tails;
    public Sprite[] halfs;

    public Sprite wall;
    public Sprite trap;
    public Sprite closedexit;
    public Sprite openexit;
    public Sprite chili;
    public Sprite banana;
    public Sprite ice;

    private const int maxsizex = 31, maxsizey = 17;
    private int sizex0, sizex1, sizey0, sizey1;

    private float spittime = 0.1f;

    public enum Back
    {
        none,
        ground,
        trap,
        exit,
    }
    public enum Item
    {
        none,
        wall,
        chili,
        banana,
        ice,
    }

    public enum Status
    {
        still,
        eatingchili,
        spitting,
        spitted,
        eatingbanana,
        falling,
        dropping,
        exiting,

        win,
        lose,
    }
    private Back[,] back;
    private Item[,] item;

    private int foodcnt;


    private Status status = Status.still;

    private Queue<GameObject> unuseds = new Queue<GameObject>();
    private Queue<GameObject> usings = new Queue<GameObject>();

    private Queue<Point> snake = new Queue<Point>();
    private List<(Point, Item)> mvit = new List<(Point, Item)>();

    private float delt;//[0,1]
    private int mdir;


    //Object Pool Part
    private GameObject GetObject()
    {
        GameObject obj;
        if (unuseds.Count > 0)
        {
            obj = unuseds.Dequeue();
            obj.SetActive(true);
        }
        else
        {
            obj = new GameObject();
            obj.AddComponent<SpriteRenderer>();
        }
        usings.Enqueue(obj);
        return obj;
    }

    private void ClearObject()
    {
        while (usings.Count > 0)
        {
            GameObject obj = usings.Dequeue();
            obj.SetActive(false);
            unuseds.Enqueue(obj);
        }

    }

    //Init Part
    void GenerateMap()
    {

        sizex0 = -12;
        sizex1 = 12;
        sizey0 = -9;
        sizey1 = 9;
        back = new Back[sizex1 - sizex0, sizey1 - sizey0];
        item = new Item[sizex1 - sizex0, sizey1 - sizey0];

        for (int i = 2; i < 14; ++i)
        {
            for (int j = 2; j < 14; ++j)
            {
                back[i, j] = Back.ground;
            }
        }
        back[5, 5] = Back.trap;
        back[6, 7] = Back.exit;


        snake.Enqueue((9, 10));
        snake.Enqueue((10, 10));
        snake.Enqueue((11, 10));
        snake.Enqueue((12, 10));

        item[4, 4] = Item.chili;
        item[6, 8] = Item.banana;
        foodcnt = 2;

        item[7, 8] = Item.wall;
        item[5, 6] = Item.wall;
        item[8, 8] = Item.ice;
    }

    //Logic Part

    int CalcDir(Point a, Point b)
    {
        if (a.Item1 == b.Item1)
        {
            if (a.Item2 < b.Item2)
            {
                return 3;
            }
            else if (a.Item2 > b.Item2)
            {
                return 1;
            }
            else
            {
                return 4;
            }
        }
        else if (a.Item2 == b.Item2)
        {
            if (a.Item1 < b.Item1)
            {
                return 0;
            }
            else if (a.Item1 > b.Item1)
            {
                return 2;
            }
        }
        return -1;
    }
    Point CalcPoint(Point a, int dir)
    {
        Point b = a;
        switch (dir)
        {
            case 0:
                ++b.Item1;
                break;
            case 1:
                --b.Item2;
                break;
            case 2:
                --b.Item1;
                break;
            case 3:
                ++b.Item2;
                break;
        }
        return b;
    }

    bool OutRange(Point p)
    {
        return p.Item1 < 0 || p.Item1 >= sizex1 - sizex0 ||
            p.Item2 < 0 || p.Item2 >= sizey1 - sizey0;
    }
    bool WillFall()
    {
        foreach (var (x, y) in snake)
        {
            if (back[x, y] != Back.none) return false;
        }
        return true;

    }
    List<Point> Pushable(Point pt, int dir, bool body)
    {
        List<Point> lst = new List<Point>();
        Point p = pt;
        while (true)
        {
            if (snake.Contains(p) && p != snake.First())
            {
                return body ? null : lst;
            }
            if (OutRange(p)) return lst;
            switch (item[p.Item1, p.Item2])
            {
                case Item.wall:
                    return null;
                case Item.none:
                    return lst;
            }
            lst.Add(p);
            p = CalcPoint(p, dir);
        }
    }

    void AddMvit(int x, int y)
    {
        mvit.Add(((x, y), item[x, y]));
        item[x, y] = Item.none;
    }
    bool Spitable()
    {
        foreach (Point p in snake)
        {
            if (Pushable(CalcPoint(p, mdir), mdir, false) == null)
            {
                return false;
            }
        }
        foreach (Point p in snake)
        {
            var lst = Pushable(CalcPoint(p, mdir), mdir, false);
            foreach (var (x, y) in lst)
            {
                AddMvit(x, y);
            }
        }
        return true;
    }


    void Move(int dir)
    {
        if (status != Status.still) return;
        Point[] s = snake.ToArray();
        int n = s.Length - 1;//[0,n]
        int hdir = CalcDir(s[n - 1], s[n]);
        int rdir = (dir + 2) % 4;

        var (nx, ny) = CalcPoint(s[n], dir);

        if (hdir == rdir || OutRange((nx, ny))) return;


        var lst = Pushable((nx, ny), dir, true);


        if (lst != null)//if pushable
        {
            snake.Dequeue();
            snake.Enqueue((nx, ny));
            if (back[nx, ny] == Back.exit && foodcnt == 0)
            {
                status = Status.exiting;
                return;
            }
            if (back[nx, ny] == Back.trap)
            {
                status = Status.exiting;
                return;
            }

            if (lst.Count > 0)
            {
                for (int i = lst.Count - 1; i >= 0; --i)
                {
                    var (x, y) = lst[i];
                    var (x1, y1) = CalcPoint(lst[i], dir);
                    item[x1, y1] = item[x, y];
                    item[x, y] = Item.none;
                }
                var (lx, ly) = CalcPoint(lst.Last(), dir);
                if (item[lx, ly] != Item.none && back[lx, ly] == Back.none)
                {
                    delt = 0;
                    status = Status.dropping;
                    mdir = 4;
                    AddMvit(lx, ly);
                }
            }

            if (WillFall())
            {
                delt = 0;
                status = Status.falling;
                mdir = 4;
            }
            else if (mvit.Count == 0)
            {
                status = Status.still;
            }


        }
        else //if not pushable
        {
            switch (item[nx, ny])
            {
                case Item.chili:
                    delt = 0;
                    status = Status.eatingchili;
                    mdir = rdir;
                    item[nx, ny] = Item.none;
                    snake.Dequeue();
                    snake.Enqueue((nx, ny));
                    --foodcnt;
                    break;
                case Item.banana:
                    delt = 0;
                    status = Status.eatingbanana;
                    item[nx, ny] = Item.none;
                    snake.Enqueue((nx, ny));
                    --foodcnt;
                    break;
            }
        }

    }

    //Paint Part

    (float, float) CalcD(Point p)
    {
        switch (mdir)
        {
            case 0:
                return (delt / spittime, 0);
            case 1:
                return (0, -delt / spittime);
            case 2:
                return (-delt / spittime, 0);
            case 3:
                return (0, delt / spittime);
            case 4:
                return (0, -delt * delt * 20);
        }
        return (0, 0);
    }

    void PaintSprite(Sprite sprite, int x, int y, float z, bool movable)
    {
        GameObject go = GetObject();
        go.GetComponent<SpriteRenderer>().sprite = sprite;
        var (dx, dy) = movable ? CalcD((x, y)) : (0, 0);
        go.transform.localPosition = new Vector3(x + sizex0 + dx, y + sizey0 + dy, z);
    }

    void ShowBack()
    {
        for (int i = 0; i < sizex1 - sizex0; ++i)
        {
            for (int j = 0; j < sizey1 - sizey0; ++j)
            {
                if (back[i, j] != Back.none)
                {
                    PaintSprite(grounds[(i + j) % 2], i, j, j + 100, false);
                }
                float layer = 80;
                switch (back[i, j])
                {
                    case Back.trap:
                        PaintSprite(trap, i, j, j + layer, false);
                        break;
                    case Back.exit:
                        PaintSprite(foodcnt > 0 ? closedexit : openexit, i, j, j + layer, false);
                        break;
                }

            }
        }
    }
    void ShowSnake()
    {
        Point[] s = snake.ToArray();
        int n = s.Length - 1;//[0,n]

        float layer = 60;
        if (status == Status.falling) layer = 50;


        int hdir = CalcDir(s[n - 1], s[n]);
        int tdir = CalcDir(s[0], s[1]);

        bool movable = false;
        {
            var (i, j) = s[n];
            switch (status)
            {
                case Status.still:
                case Status.dropping:
                    PaintSprite(moveheads[hdir], i, j, j + layer, movable = false);
                    break;
                case Status.falling:
                    PaintSprite(moveheads[hdir], i, j, j + layer, movable = true);
                    break;
                case Status.eatingchili:
                    PaintSprite(stunheads[hdir], i, j, j + layer, movable = false);
                    break;
                case Status.eatingbanana:
                    PaintSprite(smileheads[hdir], i, j, j + layer, movable = false);
                    break;
                case Status.spitting:
                    PaintSprite(tearheads[hdir], i, j, j + layer, movable = true);
                    break;
                case Status.spitted:
                    PaintSprite(tearheads[hdir], i, j, j + layer, movable = false);
                    break;
                case Status.exiting:
                    PaintSprite(exitings[snake.Count > 1 ? hdir : 4], i, j, j + layer, movable = false);
                    break;
            }

        }
        PaintSprite(tails[tdir], s[0].Item1, s[0].Item2, s[0].Item2 + layer, movable);

        for (int k = 1; k < n; ++k)
        {
            int d1 = CalcDir(s[k], s[k + 1]);
            int d2 = CalcDir(s[k - 1], s[k]);
            var (i, j) = s[k];


            if (d1 == d2)
            {
                PaintSprite(straights[d1], i, j, j + layer, movable);
            }
            else
            {
                if (d1 == (d2 + 1) % 4)
                {
                    PaintSprite(turns[d1], i, j, j + layer, movable);
                }
                else
                {
                    PaintSprite(turns[(d2 + 2) % 4], i, j, j + layer, movable);
                }

            }
        }
    }

    void ShowItems()
    {
        float layer = 60;
        for (int i = 0; i < sizex1 - sizex0; ++i)
        {
            for (int j = 0; j < sizey1 - sizey0; ++j)
            {
                switch (item[i, j])
                {
                    case Item.wall:
                        PaintSprite(wall, i, j, j + layer, false);
                        break;
                    case Item.ice:
                        PaintSprite(ice, i, j, j + layer, false);
                        break;
                    case Item.chili:
                        PaintSprite(chili, i, j, j + layer, false);
                        break;
                    case Item.banana:
                        PaintSprite(banana, i, j, j + layer, false);
                        break;
                }
            }
        }
        layer = (status == Status.dropping || status == Status.falling) ? 30 : 8;
        foreach (var ((i, j), it) in mvit)
        {

            switch (it)
            {
                case Item.wall:
                    PaintSprite(wall, i, j, j + layer, true);
                    break;
                case Item.ice:
                    PaintSprite(ice, i, j, j + layer, true);
                    break;
                case Item.chili:
                    PaintSprite(chili, i, j, j + layer, true);
                    break;
                case Item.banana:
                    PaintSprite(banana, i, j, j + layer, true);
                    break;
            }
        }
    }
    void Show()
    {
        ClearObject();

        if (status == Status.win)
        {

            return;
        }
        if (status == Status.lose)
        {

            return;
        }

        ShowBack();
        ShowSnake();
        ShowItems();

    }



    void CalcInput()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            Move(3);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            Move(1);
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            Move(2);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            Move(0);
        }
    }

    void UpdateState()
    {
        delt += Time.deltaTime;
        switch (status)
        {
            case Status.eatingchili:
                if (delt > 0.3)
                {
                    delt = 0;
                    if (Spitable())
                    {
                        status = Status.spitting;
                    }
                    else
                    {
                        status = Status.spitted;
                    }
                }
                break;
            case Status.spitting:
                if (delt > spittime)
                {
                    delt = 0;
                    foreach (var ((x, y), it) in mvit)
                    {
                        var (nx, ny) = CalcPoint((x, y), mdir);
                        if (!OutRange((nx, ny))) item[nx, ny] = it;
                    }
                    var lst = snake.ToList();
                    snake.Clear();
                    bool org = false;
                    foreach (var p in lst)
                    {
                        Point q = CalcPoint(p, mdir);
                        if (OutRange(q))
                        {
                            org = true;
                            break;
                        }
                        snake.Enqueue(q);
                    }
                    if (org)
                    {
                        status = Status.lose;
                        break;
                    }
                    mvit.Clear();
                    if (!Spitable()) status = Status.spitted;
                }
                break;
            case Status.eatingbanana:
                if (delt > 0.5)
                {
                    delt = 0;
                    status = Status.still;
                }
                break;
            case Status.falling:

                if (delt > 1)
                {
                    delt = 0;
                    status = Status.lose;
                    mvit.Clear();
                }
                break;
            case Status.dropping:

                if (delt > 1)
                {
                    delt = 0;
                    status = Status.still;
                    mvit.Clear();
                }
                break;

            case Status.spitted:
                if (delt > 0.5)
                {
                    delt = 0;
                    status = Status.still;

                }
                break;
            case Status.exiting:
                if (delt > 0.15)
                {
                    delt = 0;
                    snake.Dequeue();
                    if (snake.Count == 0)
                    {
                        status = Status.win;

                    }
                }
                break;
        }
    }

    void Start()
    {
        GenerateMap();


    }

    void Update()
    {
        CalcInput();
        UpdateState();
        Show();

    }
}
