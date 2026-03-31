Please use a machine translator if you do not speak Hungarian!

Bevezető
--------
Egy szokásos N-test szimuláció kiegészítve két extrával:
-	A tér lehet wrapped (Pacman stílusú)
-	Tágulhat a tér (kozmikus tágulás szimulációja)
Továbbá csak CPU-ra van megoldva, de képes több szálon futni, hogy gyorsítsa a munkát. GPU-t sajnos nem tud.

Wrapped (Pacman) tér koordináták
--------------------------------
A wrapped tér azt jelenti, hogy nincs igazából széle a térnek, hanem visszafordul magába, mint a Pacman játék (csak most 3D-ben).
Maga a megvalósítás nem bonyolult. Legyen mindegyik koordináta a [0, 1] tartományban! (Azaz a világ mérete egy egység.) Ekkor két számításra kell figyelni:

 1. Mikor odébb mozdul a részecske, akkor a helyét kell wrappolni.
 2. Mikor távolságot számítunk, akkor is úgymond át kell wrappolni a távolságot.

Ezeket meg lehetne oldani sima „if then else” típusú kódokkal, de a modern CPU-k esetében sokkal jobb a breanchless kód. Ezt könnyen meg lehet csinálni, csak a floor() függvénnyel kell operálni. Legalábbis ez a legjobb megoldás a C#-ban szerintem. A trükköt lásd a kódban: Wrap(double x) és WrapDiff(double dx) függvényeknél!

Wrapped (Pacman) tér anizotrópia probléma
------------------------------------------
Van egy súlyos probléma még a Wrapped térrel. Az, hogy nem izotróp a tér-egységkocka. A csúcsok iránya felől átlagosan (homogén esetben) nagyobb gravitációs hatás jön, mint a másik irányokból.

Tehát hiába nincs „közepe” és „széle” a wrapped (pacman) világnak, de maga az univerzum mégse izotróp! Ami fizikailag helytelen.
Ezt úgy lehet lekezelni, hogy minden részecske esetén csak az egység átmérőjű gömbön belüli többi részecske vonzását számoljuk el. (Azaz fél sugáron belülit.)

Tágulás elszámolása
-------------------
Maga a tér tágulása könnyen programozható lenne pl. így:

 1. Megnöveljük a világ méretét és vele a részecskék koordinátáját átskálázzuk. (Pl. mostantól nem [0, 1] lesz a koordináta tartomány, hanem [-0.01, 1.01] és arányosan eltoljuk a részecskéket a (0.5, 0.5, 0.5) középponttól.)
 2. És a sebességeket meg arányosan lecsökkentjük. (Ez fizikai hatása a tágulásnak.)

De ha így járnánk el, akkor wrapped függvényeket kellene elbonyolítani, ami feleslegesen lassítani és bonyolítaná a kódot.
Ezért helyette nem skálázom át a koordinátákat, hanem a gravitációs hatás számításában (DoForceCalculation) a számolt távolságokat növelem fel. Vagyis, hogy még gyorsabb legyen a kód, egyszerűen a G állandót csökkentem le négyzetesen.

Természetese a softening paramétert is arányosan kell csökkenteni az erő számításakor. (Ha nem tudod mi ez, akkor nézz után a szakirodalomban!)

A sebességeket ezért kétszer kell csökkenteni: egyszer a fenti fizikai oka miatt, másrészt mert a tágulással nem növeljük meg a koordinátákat (Ld. a DoExpansion-ban).

Mikor számoljuk el a tágulás sebességre vett hatását?
-----------------------------------------------------
A nehezebb probléma, hogy hol végezzük el a sebesség változtatását. Az N-test számítás lépései a HalfKick ,Drift, Force számítás, második half-kick. (Ha nem tudod miért, akkor nézz után a szakirodalomban!)

Ez azért probléma, mert ha a tágulás hatását (sebességek csökkentése) egy időlépés végén (vagy elején) hajtanánk végre, akkor az első half-kick + drift másféle koordinátarendszerben (kissé tágult) dolgozna, mint a második half-kick. Bár ez nagyon minimális hatással lenne a számításokra, de mégse helyes fizikai szempontból. Ezért a tágulás hatását a drift után a gyorsulás számítás elé raktam be.

Ez azért is jó, mert felmerülhet a kérdés, hogy a gyorsulásokat nem kellene átskálázni a tágulással? És ha igen, akkor hogyan? Ezt nem tudom biztosan; de így nem is kell. Mert a DoExpansion után úgyis teljesen új gyorsulásokat számolunk a DoForceCalculation-nel.

Igaz, hogy magát a scale változót ténylegesen az időlépés végén növelem meg (DoSecondHalfKick után); de ez nem számít semmit se, mert nem befolyásolja a következő lépés half-kick + drift-jét. (Azért tettem ide a kódban, mert a scale változtatása globális, nem osztható szét a thread-ek között, ahogy az idő növelése se. Ha a DoExpansion-ba tettem volna, akkor mindegyik thread megnövelte volna a skálafaktort…)

Többszálúság megvalósítása
--------------------------
A szokásos könnyű megoldás az, hogy szétosztjuk az N darab részecskét az X darab szál (számítási mag) között, és mindegyik teljesen kiszámolja az i-edik részecskére ható összes (j = 0 … N-1, j != i) részecske gravitációs hatását. Ha sok számítási szál van, akkor ez ésszerű megoldás, de ha csak pár (CPU esetében), akkor ezzel dupla annyi számítást csinálunk, mint kellene.
Ugyanis, ha kiszámoltuk, hogy mekkora gyorsulás hat i-re az j-ből, akkor azt is tudjuk (csak másik tömeggel kell szorozni), hogy mennyi hat j-re az i-ből. Amikor egy szálon fut a számítás, akkor pontosan így is csinálják. De ha több szálon fut, akkor két probléma is felmerül:

1.	Thread ütközés, ha két szál egyidőben adna hozzá számolt gyorsulást ugyanahhoz az indexű részecskéhez
2.	Nem igazságos a munka szétosztása, ha csak simán egyenlően szétosztjuk az N-darab részecskét a szálak között.
	
Elvileg a C# (.Net) parrallel.foreach vagy hasonló trükkje tudná mindezt kezelni, de ez nem valami hatékony megoldás (kipróbáltam). Jobb, ha van pár prezisztens thread-ünk, és azok dolgoznak a számításon (azért is, mert majd a CPU, a cache, stb. szépen meg fogja „tanulni”, hogy ezek a szálak min dolgoznak sokat).
Tehát legyen mondjuk 4 darab szálunk, amik dolgoznak!

A thread ütközést (1. probléma) könnyen lehet kezelni, csak annyit kell csinálni, hogy mindegyik szál saját buffer-be írja a számolt gyorsulásokat, és mikor mindegyik szál végzett, akkor összeadjuk őket. Ez egy kis extra memóriát igényel, de az igény elhanyagolhatóan csekély (amúgy még optimalizálni is lehetne, de nem fáradtam vele).

A második kicsit cselesebb. Mivel a gyorsulás számolás belső ciklusa for j = i, .., N fut, a ezért az első szál sokkal többet dolgozna, ha a külső ciklusa ugyanannyit futna, mint a 2., 3., .. szálé. Ezért a gyorsulás számolás esetében (ami a MUNKAIGÉNYES rész) külső ciklus index tartományát speciális módon számoljuk ki, hogy egyenlőek legyenek a terhelések. Lásd a kódot!

Paraméterek
-----------
Az NBodySimulation elején van egy sor konstans paraméter, amiket be kell állítani egy konkrét futás előtt. A legtöbb paraméter magáért beszél.
Ami lényeges, hogy hogyan állítsuk be a G és DT és EXPANSION_FACTOR paramétert. A G és DT önmagában nem jelent semmit, hiszen a szimuláció dimenziómentesített (nincs mértékegység). Ezért nem az számít, hogy konkrétan mennyi a G, hanem a G és DT aránya számít. Ha G kicsi, akkor DT lehet nagyobb és fordítva. A lényeg az, hogy elég kicsi legyen a DT, hogy egy időlépésben csak kicsit gyorsuljanak és mozogjanak a részecskék. Erre valamennyire a SOFTENING is hatással van, azt se szabad túl kicsire beállítani, túl nagy meg nem lesz elég realisztikus.

Tapasztalatom szerint olyan 1000-2000 részecskeszámnál (N), a DT kb. 1E-6*G és 1E-5*G között legyen.

A tágulás lineáris, és a EXPANSION_FACTOR adja meg, hogy mennyit tágul egy időegység alatt. Tehát ha a DT = 1E-6 és az EXPANSION_FACTOR = 100, akkor egy időlépés (1E-6 időegység) alatt a tér 1E-4 távolságegységgel növekszik meg. Vagyis növekedne meg, ha úgy számolnánk, hogy a koordinátákat növeljük, de mint szó volt róla igazából a távolságokat csökkentjük a skálával.

Egyéb
-----
Animációt direkt nem tud csinálni a program. Helyette PNG képekbe tudja lementeni az előállított 2D/3D renderelt frame-eket. A Form1 vezérli az egészet. Jelenleg úgy van beállítva (ez is egy konstans), hogy 100 időlépés után csinál egy új képet, amit megjelenít az ablakban (képernyőn) és user választása szerint lementi PNG fájlba is, adott mappába.
A PNG képekből (frame0000.png, frame0001.png, …) videót valamilyen külső programmal lehet csinálni. Ajánlom az ingyenes mplayer-t (mencoder)!
