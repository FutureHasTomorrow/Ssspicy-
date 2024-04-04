using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Xml.Schema;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Point = System.ValueTuple<int, int>;
using Random = System.Random;

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
    public Sprite[] trappings;
    public Sprite[] straights;
    public Sprite[] turns;
    public Sprite[] tails;
    public Sprite[] halfs;

    public GameObject wall;
    public GameObject trap;
    public GameObject exit;
    public GameObject chili;
    public GameObject banana;
    public GameObject ice;
    public GameObject wood;
    public GameObject fire;
    public GameObject dust;

    public AudioSource audiosrc;
    public AudioSource spitsrc;

    public AudioClip[] eatsounds;
    public AudioClip[] movesounds;
    public AudioClip growsound;
    public AudioClip trapsound;
    public AudioClip exitsound;
    public AudioClip pushsound;
    public AudioClip fallsound;

    private const int maxsizex = 31, maxsizey = 17;
    private int sizex0, sizex1, sizey0, sizey1;

    private float groundz = 40;
    private float backz = 38;
    private float itemz = 10;
    private float snakez = 10;
    private float fallz = 100;

    private float spittime = 0.1f;

    private CameraVib camvib;

    private Random rand = new Random();
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
        trapped,

        win,
        lose,
    }
    private Status status = Status.still;

    private GameObject[,] ground;
    private GameObject[,] back;
    private GameObject[,] item;
    private GameObject exitobj;
    private GameObject fireobj;
    private GameObject dustobj;

    private LinkedList<(GameObject, Point, int)> skl = new LinkedList<(GameObject, Point, int)>();
    private List<(GameObject, Point)> mvitem = new List<(GameObject, Point)>();


    private int foodcnt;

    private float delt;//[0,1]
    private int mdir;

    Vector3 CalcSnakeVec(Point p)
    {
        return new Vector3(p.Item1 + sizex0, p.Item2 + sizey0, p.Item2 + snakez);
    }
    GameObject CreateSnakeObject(Sprite s, Point p)
    {
        GameObject obj = new GameObject();
        obj.AddComponent<SpriteRenderer>().sprite = s;
        obj.transform.localPosition = CalcSnakeVec(p);
        obj.name = "snake";
        return obj;
    }
    int GetHeadDir()
    {
        return skl.Last().Item3;
    }
    void SetHeadSprites(Sprite[] s)
    {
        skl.Last().Item1.GetComponent<SpriteRenderer>().sprite = s[GetHeadDir()];
    }

    //Calculation
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

    private Sprite CalcBodySprite(int d0, int d1)
    {
        if (d1 == d0)
        {
            return straights[d1];
        }
        else if (d1 == (d0 + 1) % 4)
        {
            return turns[d1];
        }
        else
        {
            return turns[(d0 + 2) % 4];
        }
    }


    void InitSnake(List<Point> s)
    {
        //The initial length of the snake is at least 3
        int n = s.Count - 1;
        skl.AddLast((CreateSnakeObject(tails[CalcDir(s[0], s[1])], s[0]), s[0], 0));
        for (int i = 1; i < n; ++i)
        {
            int d0 = CalcDir(s[i - 1], s[i]);
            int d1 = CalcDir(s[i], s[i + 1]);
            skl.AddLast((CreateSnakeObject(CalcBodySprite(d0, d1), s[i]), s[i], d0));
        }
        int d = CalcDir(s[n - 1], s[n]);
        skl.AddLast((CreateSnakeObject(moveheads[d], s[n]), s[n], d));
    }
    void SnakeHead(Point p)//change head 
    {
        var last = skl.Last();
        Point f = last.Item2;
        int d0 = last.Item3;
        int d1 = CalcDir(f, p);
        last.Item1.GetComponent<SpriteRenderer>().sprite = CalcBodySprite(d0, d1);
        skl.AddLast((CreateSnakeObject(moveheads[d1], p), p, d1));
    }
    void SnakeTail()
    {
        Destroy(skl.First().Item1);
        skl.RemoveFirst();

        if (skl.Count == 1)
        {
            if(status==Status.exiting)
            {
                skl.First().Item1.GetComponent<SpriteRenderer>().sprite = exitings[4];
            }
            else if(status==Status.trapped)
            {
                skl.First().Item1.GetComponent<SpriteRenderer>().sprite = trappings[4];
            }
        }
        else if (skl.Count > 1)
        {
            skl.First().Item1.GetComponent<SpriteRenderer>().sprite = tails[skl.ElementAt(1).Item3];
        }

    }
    void AddGround(Point p)
    {
        var (i, j) = p;
        ground[i, j] = new GameObject();
        ground[i, j].AddComponent<SpriteRenderer>().sprite = grounds[(i + j) % 2];
        ground[i, j].transform.localPosition = new Vector3(i + sizex0, j + sizey0, j + groundz);
        ground[i, j].name = "ground";
    }
    GameObject CreateObject(GameObject prefab, Point p, float z, string name)
    {
        GameObject obj = Instantiate(prefab);
        obj.transform.localPosition = new Vector3(p.Item1 + sizex0, p.Item2 + sizey0, p.Item2 + z);
        obj.name = name;
        return obj;
    }

    void GenerateMap()
    {
        sizex0 = -24;
        sizex1 = 24;
        sizey0 = -15;
        sizey1 = 15;

        ground = new GameObject[sizex1 - sizex0, sizey1 - sizey0];
        back = new GameObject[sizex1 - sizex0, sizey1 - sizey0];
        item = new GameObject[sizex1 - sizex0, sizey1 - sizey0];


        InitSnake(new List<Point> { (18, 13), (19, 13), (20, 13), (21, 13) });


        for (int i = 14; i < 26; ++i)
            for (int j = 8; j < 20; ++j)
                AddGround((i, j));

        back[17, 11] = CreateObject(trap, (17, 11), backz, "trap");
        exitobj = back[18, 13] = CreateObject(exit, (18, 13), backz, "exit");

        foodcnt = 2;
        item[16, 10] = CreateObject(chili, (16, 10), itemz, "chili");
        item[18, 14] = CreateObject(banana, (18, 14), itemz, "banana");
        item[19, 14] = CreateObject(wall, (19, 14), itemz, "wall");
        item[17, 12] = CreateObject(wall, (17, 12), itemz, "wall");
        item[20, 14] = CreateObject(ice, (20, 14), itemz, "ice");
    }

    void AddMvitem(int x, int y)
    {
        mvitem.Add((item[x, y], (x, y)));
        item[x, y] = null;
    }

    bool OutOfRange(Point p)
    {
        return p.Item1 < 0 || p.Item1 >= sizex1 - sizex0 ||
            p.Item2 < 0 || p.Item2 >= sizey1 - sizey0;
    }
    bool SnakeFall()
    {
        foreach (var (obj, (x, y), d) in skl)
        {
            if (ground[x, y] != null) return false;
        }
        return true;

    }
    List<Point> Pushable(Point pt, int dir, bool body)
    {
        List<Point> lst = new List<Point>();
        Point p = pt;

        while (true)
        {
            if (OutOfRange(p)) return lst;
            var emu = skl.GetEnumerator(); emu.MoveNext();
            while (emu.MoveNext())
            {
                if (emu.Current.Item2 == p)
                {
                    return body ? null : lst;
                }
            }
            GameObject obj = item[p.Item1, p.Item2];
            if (obj == null) return lst;
            if (obj.name == "wall") return null;
            lst.Add(p);
            p = CalcPoint(p, dir);
        }
    }
    bool Spitable()
    {
        foreach (var (obj, p, d) in skl)
        {
            var lst = Pushable(CalcPoint(p, mdir), mdir, false);
            if (lst == null)
            {
                mvitem.Clear();
                return false;
            }
            foreach (var (x, y) in lst)
            {
                AddMvitem(x, y);
            }
        }
        return true;
    }

    void SetStatus(Status s)
    {
        status = s;
        delt = 0;
        switch (s)
        {
            case Status.still:
                SetHeadSprites(moveheads);
                break;
            case Status.falling:
                SetHeadSprites(sweatheads);
                break;
            case Status.trapped:
                SetHeadSprites(trappings);
                break;
            case Status.exiting:
                SetHeadSprites(exitings);
                break;
            case Status.spitting:
            case Status.spitted:
                SetHeadSprites(tearheads);
                break;
            case Status.eatingbanana:
                SetHeadSprites(smileheads);
                break;
            case Status.eatingchili:
                SetHeadSprites(stunheads);
                break;
        }
    }
    void Move(int dir)
    {
        if (status != Status.still) return;

        int hdir = skl.Last().Item3;
        int rdir = (dir + 2) % 4;

        Point np = CalcPoint(skl.Last().Item2, dir);
        var (nx, ny) = np;

        if (hdir == rdir || OutOfRange(np)) return;

        var lst = Pushable(np, dir, true);

        if (lst != null)//if pushable
        {
            SnakeHead(np);
            SnakeTail();
            if(dustobj!=null) Destroy(dustobj);
            dustobj = CreateObject(dust, skl.First().Item2, skl.First().Item2.Item2 + snakez, "dust");

            if (lst.Count > 0)
            {
                audiosrc.PlayOneShot(pushsound);
                for (int i = lst.Count - 1; i >= 0; --i)
                {
                    var (x, y) = lst[i];
                    var (x1, y1) = CalcPoint(lst[i], dir);
                    item[x1, y1] = item[x, y];
                    item[x1, y1].transform.localPosition = new Vector3(x1 + sizex0, y1 + sizey0, y1 + itemz);
                    item[x, y] = null;
                }
                var (lx, ly) = CalcPoint(lst.Last(), dir);
                if (item[lx, ly] != null && ground[lx, ly] == null)
                {
                    SetStatus(Status.dropping);
                    audiosrc.PlayOneShot(fallsound);
                    mdir = 4;
                    AddMvitem(lx, ly);
                }
            }
            else
            {
                audiosrc.PlayOneShot(movesounds[rand.Next(0, movesounds.Length)]);
            }

            if (back[nx, ny]?.name == "exit" && foodcnt == 0)
            {
                SetStatus(Status.exiting);
            }
            else if (back[nx, ny]?.name == "trap")
            {
                SetStatus(Status.trapped);
                audiosrc.PlayOneShot(trapsound);
            }
            else if (SnakeFall())
            {
                SetStatus(Status.falling);
                audiosrc.PlayOneShot(fallsound);
                mdir = 4;
            }
            else if (mvitem.Count == 0)
            {
                SetStatus(Status.still);
            }
        }
        else //if not pushable
        {
            switch (item[nx, ny]?.name)
            {
                case "chili":
                    SnakeHead(np);
                    SnakeTail();


                    SetStatus(Status.eatingchili);
                    audiosrc.PlayOneShot(eatsounds[rand.Next(0, eatsounds.Length)]);
                    mdir = rdir;

                    Destroy(item[nx, ny]);
                    item[nx, ny] = null;
                    --foodcnt;
                    if(foodcnt==0)
                    {
                        exitobj.GetComponent<Animator>().SetBool("open", true);
                    }
                    spitsrc.Play();

                    break;
                case "banana":
                    SnakeHead(np);

                    SetStatus(Status.eatingbanana);
                    audiosrc.PlayOneShot(eatsounds[rand.Next(0, eatsounds.Length)]);

                    Destroy(item[nx, ny]);
                    item[nx, ny] = null;
                    --foodcnt;
                    if (foodcnt == 0)
                    {
                        exitobj.GetComponent<Animator>().SetBool("open", true);
                    }
                    audiosrc.PlayOneShot(growsound);


                    break;
            }
        }

    }

    //Paint Part

    (float, float) CalcOffset()
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

    void CalcInput()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            Move(3);
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            Move(1);
        }
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Move(2);
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            Move(0);
        }
    }

    void UpdateState()
    {
        var (dx, dy) = CalcOffset();
        delt += Time.deltaTime;
        switch (status)
        {
            case Status.eatingchili:
                if (delt > 0.3)
                {
                    SetStatus(Spitable() ? Status.spitting : Status.spitted);
                    fireobj = CreateObject(fire, skl.Last().Item2, snakez, "fire");
                    fireobj.transform.localEulerAngles = new Vector3(0, 0, -(skl.Last().Item3) * 90);
                    camvib.StartVibration();

                }
                break;
            case Status.spitting:
                Point fp = CalcPoint(skl.Last().Item2, skl.Last().Item3);
                fireobj.transform.localPosition = new Vector3(fp.Item1 + sizex0 + dx, fp.Item2 + sizey0 + dy, snakez);
                foreach (var (obj, p) in mvitem)
                {
                    obj.transform.localPosition = new Vector3(p.Item1 + sizex0 + dx, p.Item2 + sizey0 + dy, itemz);
                }
                foreach (var (obj, p, d) in skl)
                {
                    obj.transform.localPosition = new Vector3(p.Item1 + sizex0 + dx, p.Item2 + sizey0 + dy, snakez);
                }
                if (delt > spittime)
                {
                    foreach (var (obj, p) in mvitem)
                    {
                        Point q = CalcPoint(p, mdir);
                        if (!OutOfRange(q)) item[q.Item1, q.Item2] = obj;
                    }
                    var lst = skl.ToList();
                    skl.Clear();
                    bool ofr = false;
                    foreach (var (obj, p, d) in lst)
                    {
                        Point q = CalcPoint(p, mdir);
                        if (OutOfRange(q)) ofr = true;
                        obj.transform.localPosition = CalcSnakeVec(q);
                        skl.AddLast((obj, q, d));
                    }
                    mvitem.Clear();
                    if (ofr)
                    {
                        SetStatus(Status.lose);
                        camvib.StopVibration();
                        spitsrc.Stop();
                        Destroy(fireobj);
                    }
                    else SetStatus(Spitable() ? Status.spitting : Status.spitted);
                }
                break;
            case Status.spitted:
                if (delt > 0.5)
                {
                    SetStatus(Status.still);
                    camvib.StopVibration();
                    Destroy(fireobj);
                    spitsrc.Stop();
                }
                break;
            case Status.eatingbanana:
                if (delt > 0.5)
                {
                    SetStatus(Status.still);
                }
                break;
            case Status.falling:
                foreach (var (obj, p) in mvitem)
                {
                    obj.transform.localPosition = new Vector3(p.Item1 + sizex0 + dx, p.Item2 + sizey0 + dy, fallz);
                }
                foreach (var (obj, p, d) in skl)
                {
                    obj.transform.localPosition = new Vector3(p.Item1 + sizex0 + dx, p.Item2 + sizey0 + dy, fallz);
                }
                if (delt > 1)
                {
                    SetStatus(Status.lose);
                    mvitem.Clear();
                }
                break;
            case Status.dropping:
                foreach (var (obj, p) in mvitem)
                {
                    obj.transform.localPosition = new Vector3(p.Item1 + sizex0 + dx, p.Item2 + sizey0 + dy, fallz);
                }
                if (delt > 1)
                {
                    SetStatus(Status.still);
                    mvitem.Clear();
                }
                break;
            case Status.exiting:
                if (delt > 0.15)
                {
                    SetStatus(Status.exiting);
                    SnakeTail();
                    if (skl.Count == 0)
                    {
                        SetStatus(Status.win);
                        exitobj.GetComponent<Animator>().SetBool("open", false);
                        audiosrc.PlayOneShot(exitsound);

                    }
                }
                break;
            case Status.trapped:
                if (delt > 0.15)
                {
                    SetStatus(Status.trapped);
                    SnakeTail();
                    if (skl.Count == 0)
                    {
                        SetStatus(Status.lose);
                    }
                }
                break;
        }
    }

    void Start()
    {
        GenerateMap();
        camvib = GameObject.Find("Main Camera").GetComponent<CameraVib>();
    }

    void Update()
    {
        CalcInput();
        UpdateState();
    }
}
