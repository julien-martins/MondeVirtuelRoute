# Monde Virtuel

Julien Percheron, Nils Carron, Julien Martins

## Objectifs

- Route generee par A* + route subsidiaire par L-system
- Intersection / Smooth
- Topologie / Pont / Tunnel
## Links
https://martindevans.me/game-development/2015/12/11/Procedural-Generation-For-Dummies-Roads/ \
Un algo de génération de route expliqué simplement

https://www.youtube.com/watch?v=fI8VQV6i_Ws \
Inspiration: Generateur de route de Houdini 

https://www.reddit.com/r/proceduralgeneration/comments/92p0xq/procedurally_generated_road/ \
Exemple de quelqu'un qui a codé en Python une génération procédurale de route en se basant sur a*

https://tel.archives-ouvertes.fr/tel-00841373/document \
These de Adrien Petavie: Generation de route

https://github.com/pboechat/roadgen/blob/master/%282001%29%20Procedural%20Modeling%20of%20Cities.pdf \
Road Network 

https://www.youtube.com/playlist?list=PLcRSafycjWFcbaI8Dzab9sTy5cAQzLHoy
Implementation proposée de L-System sur Unity

## Intro
------------------------------------------------
Nous allons utiliser un L-system pour générer plusieurs réseaux de routes qui prendraient source dans une route principale.
La route principale serait générée avec un algorithme A* conditionné par un Bruit Perlin ce qui va permettre d'avoir une route qui n'est pas simplement droite.

Un L-system se base sur 3 points : la valeur d'entrée, les règles qui lui sont appliquées et un nombre d'itération.

Dans la version que nous allons implémenter, l'entrée et la sortie seront des chaînes de caractères, et les règles les différents changements que l'on peut y apporter. Il suffira alors de répéter l'opération en prenant la sortie de l'étape n-1 pour l'entrée de l'étape n.

La finalité sera une longue chaîne de caractères que l'on pourra "traduire" en un tracé sur Unity. On souhaite placer de manière aléatoire sur une route principale différents points de départ de L-system. 
## Generation de la route principale
------------------------------------------------

Pour la generation de la route principale, nous avons utilisé un algorithme d'A* qui relie un point de depart (Case vert) a un point d'arrive (Case rouge). Pour utiliser cet algorithme nous avons du mettre en place une grille de Case que l'algorithme pourra explorer.
#### Algorithme d'A Star
------------------------------------------------
L'algorithme utilisé est un simple algorithme d'A* qui cherche parmi les huit voisins à chaque itération.
La map est séparée en Node, un Node correspond a un noeud exploré par l'algoritme d'A*.

```cs
    public class Node {
        public Vector2Int Index;
        public Vector3 WorldPos;

        public int Cout;
        public int Heuristique;
        
        public Color Color;

        public Node Pred;
    }
```
```cs
void FindPath(Node start, Node end)
    {
        List<Node> closed = new();
        List<Node> open = new();

        open.Add(start);
        
        
        while (open.Count > 0)
        {
            Node u = GetSmallestNode(open);
            open.Remove(u);

            if (u.Index == end.Index) return;

            List<Node> neightbors = GetNeightbors(u);
            foreach (var neightbor in neightbors)
            {
                Node v = neightbor;
                if (!closed.Contains(v) && !open.Contains(v) && PerlinValue[v.Index.x, v.Index.y] > PerlinThreshold)
                {
                    v.Cout = u.Cout + 1;
                    v.Heuristique = v.Cout + Heuristic(v, end);
                    v.Pred = u;
                    open.Add(v);
                }
                
                closed.Add(u);
            }

        }
    }
```

#### Generation du Perlin Noise
------------------------------------------------
Nous voulons aussi que la route principale puisse prendre un aspect un peu plus hasardeux qu'une simple ligne rejoignant le point de depart et le point d'arrive.
Pour ça, nous avons eu l'idee de mettre un bruit de perlin sur la grille précédemment créé et de faire en sorte que l'algorithme d'A* esquive les zones ont le poids est important.
La génération de perlin noise a été faite grâce a la fonction PerlinNoise fournie par Unity.

```cs
public void GeneratePerlinNoise()
    {
        PerlinValue = new float[(int)Grid.cellSize.x, (int)Grid.cellSize.y];

        for (int j = 0; j < Grid.cellSize.y; ++j)
        {
            for (int i = 0; i < Grid.cellSize.x; ++i)
            {
                float x = (i / Grid.cellSize.x * PerlinScale);
                float y = (j / Grid.cellSize.y * PerlinScale);

                float val = Mathf.PerlinNoise(x, y);
                
                PerlinValue[i, j] = val;
            }
        }
    }
```
### Utilisation
------------------------------------------------
```cs
public void FindPathAction()
    {   
        GeneratePerlinNoise();
        InitializeGrid();
        FindPath(nodes[StartPoint.x, StartPoint.y], nodes[EndPoint.x, EndPoint.y]);
    }
```
GeneratePerlinNoise() -> Génère une valeur entre 0 et 1 pour chaque case du tableau (PerlinValue) qui fait la même taille que la map

InitializeGrid() -> Initialise la grille en positionnant chaque nœud, en leur assignant la valeur du bruit de perlin calculer precedement et en leur affectant une couleur liée par l'état du nœud

FindPath(Node start, Node end) => Calcule le chemin entre le point de départ et le point d'arrivée en prenant en compte le bruit de perlin généré et stocké le chemin calculé dans une variable (path).

### Rendu
------------------------------------------------
Affichage de la grille avec le point de depart(Case verte) et le point d'arrive (Case rouge)
![screen1](/screens/screen1.jpg)

Affichage du chemin en esquivant les endroits ou le poids est important
![screen2](/screens/screen3.jpg)

## Lissage de Courbe :

Nous pouvons voir qu'en raison de notre système de grille (obligatoire pour l'utilisation de notre algorithme A*) les chemins ne semblent pas très naturels, les angles pris par la route se devaient d'être aménagés.
La méthode utilisée pour lisser les courbes que nous avions générées s'est faite en plusieurs étapes.
Nous allons voir lesquels nous avons utilisées, lesquels ont étés retenues, et le résultat final

### Développement des clothoides :

Dans un premier temps, nous avons décidés de nous inspirer de ce qui se faisait avec de véritables routes. 
![screen3](/screens/road.jpg)
Ici, un échangeur d'autoroute, on peut voir les courbes tracés, ces derniers permettent à la voiture de décélérer au fur et à mesure qu'elle sort de la voie rapide.
La notion mathématique qui se cache derrière ce virage en douceur se nomme une courbe Clothoide.
Les clothoides sont des fonctions dont la courbure varie linéairement  en fonction d'une longueur d'arc.

Ces courbes sont également connus sous le nom "spirale d'Euler".

    cf https://fr.wikipedia.org/wiki/Clotho%C3%AFde
    cf https://mathcurve.com/courbes2d/cornu/cornu.shtml
### Implémentation des clothoides :

```cs
public Clothoid(double startX, double startY, double startDirection, double startCurvature, double a, double length)
        {
            _start_x = startX;
            _start_y = startY;
            _start_direction = startDirection;
            _start_curvature = startCurvature;
            _a = a;
            _length = length;

            Posture2D endPosture = InterpolatePosture2D(1.0);

            _end_x = endPosture.X;
            _end_y = endPosture.Y;
            _end_direction = endPosture.Direction;
            _end_curvature = endPosture.Curvature;
        }
```
Ce code permettait de générer une courbe clothoide, en entrant en paramètre plusieurs éléments
startX et Y sont les coordonnées du premier point de la courbe
startDirection est l'angle en radiant dans lequel commence la courbe
startCurvature est un nombre de 0 à 1 qui détermine sur quel point commence la courbe (déterminant s'il sera plus ou moins en spirale)
A est l'intensité avec laquelle la clothoide se rétracte sur elle même

![screen4](/screens/Clothoid Result.png)

On peut voir que la clothoid ainsi générée part d'un point de l'angle à lisser et suit son équation pour prolonger la route.
Cependant, les courbes clothoides ne sont pas très adaptées pour relier deux droites entre elles.

![screen5](/screens/DroiteClothoide.png)

On voit sur cette image comme cette méthode permet de relier un cercle à une droite et non deux droites ensembles. 
On comprend mieux l'espace supplémentaire que les échangeurs d'autoroutes prennent pour tourner sur un angle de 90 degrés.
En effet, le fait de prendre rapidement un angle circulaire posait le problème d'un repli trop fort pour lier deux points.

Nous avons alors pensé  à tracer deux courbes clothoides à partir de chaque point l'une vers l'autre, et de modifier la route suivant le point où elles se croisent.
Nous nous sommes rapidement aperçu que cela n'avait pas d'intérêt réel, et ne résolvait pas le problème d'un virage trop fort.
### Implémentation des courbes d'Hermite :

![screen6](/screens/BezierCurves.png) 

Nous avons donc abandonné l'utilisation des courbes clothoides, et nous sommes tournés vers des algorithmes que nous maitrisions plus, car nous avons étudiés ces derniers en cours.
Premièrement, les courbes de Bézier montraient d'excellents résultats, lorsque nous faisions varier les points à la main.
Cependant, comme on peut le voir sur l'image, il est difficile de trouver une équation qui permette de relier tous les types d'angles correctement.
Cela vient du fait que ces courbes ont besoins de quatre points pour être tracé.

```cs
    public List<Node> GenerateSmoothCurves(List<Node> errorNodes)
    {
        List<Node> result = new();

        for (int i = 0; i < errorNodes.Count; i += 3)
        {
            Vector3 p0 = errorNodes[i].WorldPos;
            Vector3 p1 = errorNodes[i + 1].WorldPos;
            Vector3 p2 = errorNodes[i + 2].WorldPos;
            
            for (float t = 0; t <= 1; t += 0.1f)
            {
                Vector3 p = QuadraticBezierCurves(t, p0, p1, p2);

                var offsetX = (Grid.cellSize.x/2) * TileSize;
                var offsetZ = (Grid.cellSize.y/2) * TileSize;
                
                Vector2Int coord = new Vector2Int((int)Mathf.Abs((p.x + offsetX) / TileSize ), (int)Mathf.Abs((p.z + offsetZ) / TileSize ));
                
                if (InMatrix(coord))
                {
                    result.Add(nodes[coord.x, coord.y]);
                }
            }
        }
        
        return result;
    }
```

Nous nous sommes donc tournés vers une autre méthode étudiée en cours, qui nécessite que trois points pour être tracé.
Dans notre cas de lissage de courbe, les trois points étaient à notre disposition.
Cette méthode est la courbe de Hermite.

![screen7](/screens/HermiteCurve.png) 

Les angles empruntés par la route semblent bien plus naturels et praticables, si bien qu'on peut aisément imaginer l'absence d'un système de grille sous-jacent.



## L-systemsBézier 

------------------------------------------------

Le principe d'un L-System est relativement simple : on fourni une "phrase" de départ composée à l'aide d'un alphabet.
On va ensuite itérer un certain nombre de fois sur cette phrase en y appliquant ce qu'on appelle des "règles".
Ces règles sont utilisées pour traduire un caractère dans une autre suite de caractères

Pour illustrer le principe :  
Prenons comme mot de départ "A", on définit également une règle qui dit "A -> AB" et une autre qui dit "B->A"
Maintenant, on peut itérer sur la phrase en y appliquant  à chaque fois nos règles.  
Cela donne donc :

    Itération 0 : A
    Itération 1 : AB
    Itération 2 : ABA
    Itération 3 : ABAAB
    (...)

### Intégration

Pour l'intégration du L-System, nous avons décidé d'une implémentation en algorithme récursif. On définit au préalable des règles à utiliser et une limite d'itération. On peut ensuite appeller la fonction avec une phrase pour obtenir en retour la version traduite à l'aide des règles.

```csharp
    // Fonction a appeller pour obtenir le resultat du L-system.
    public string GenerateSentence(string word = null)
    {
        if(word == null)
        {
            word = rootSentence;
        }

        return GrowRecursive(word);
    }

    // Si le nombre d'iterations > au nombre d'iterations max alors on renvoit le mot.
    // Sinon, on lance la recherche pour chaque caractere de la phrase pour savoir si une regle existe.
    private string GrowRecursive(string word, int iterationIndex = 0)
    {
        if(iterationIndex >= iterationLimit)
        {
            return word;
        }

        StringBuilder newWord = new StringBuilder();

        foreach(var c in word)
        {
            newWord.Append(c);
            ProcessRulesRecursivelly(newWord, c, iterationIndex);
        }

        return newWord.ToString();
    }

    // On cherche parmi toutes les règles si il en existe qui se rapporte au caractère donné.
    // Si non rien ne se passe, sinon on appelle GrowRecursive avec le mot formé par la traduction avant de l'ajouter à la phrase complète.
    private void ProcessRulesRecursivelly(StringBuilder newWord, char c, int iterationIndex)
    {
        foreach(var rule in rules)
        {
            if(rule.letter == c.ToString())
            {
                if (randomIgnoreRuleModifier && iterationIndex > 1)
                {
                    if(Random.value < chanceToIgnoreRule)
                    {
                        return;
                    }
                }
                newWord.Append(GrowRecursive(rule.GetResult(), iterationIndex + 1));
            }
        }
    }
```

Exemple d'execution :

    Phrase de base : [F]--F
    Règle de traduction : F -> [+F][-F]
    Nombre d'itérations : 3
    
    Résultat : [F[+F[+F[+F][-F]][-F[+F][-F]]][-F[+F[+F][-F]][-F[+F][-F]]]]--F[+F[+F[+F][-F]][-F[+F][-F]]][-F[+F[+F][-F]][-F[+F][-F]]]

### Application au problème de génération de route

Le principe du L-system peut s'avérer très utile pour répondre au problème de génération de route.
Dans notre cas, nous avons décidé d'exploiter la phrase générée par le L-System pour diriger un constructeur de route avec un fonctionnement similaire à une turtle.  
Une turtle est un terme utilisé pour désigner en bibliothèque graphique un agent qui fonctionne de manière similaire à un crayon.
Il trace un trait à chaque endroit où il passe et il se déplace en ligne droite. On peut également le faire tourner à droite ou a gauche. 
Enfin, il est possible de sauvegarder l'état de la turtle et de la charger plus tard. Son état correspond à sa position et à sa rotation au moment de la sauvegarde.  

On peut traduire l'alphabet utilisé par le L-System dans des instruction pour notre turtle qui sera utilisée comme constructeur de notre structure de route.

    Sauvegarder l'état = [
    Charger l'état = ]
    Avancer et dessiner = F
    Se tourner à droite = +
    Se tourner à gauche = -

En appliquant ces instructions sur le résultat du L-system présenté précédemment, on obtient le résultat graphique ci-dessous : 

![Lsystem1](/screens/L-System1.png)

### Ajout d'aléatoire pour une génération procédurale

La méthode présentée précedemment a pour avantage de ressembler déjà à un réseau de routes, mais il manque d'aléatoire.
Il faut perturber le L-System de manière à ce que la phrase de retour ne soit pas toujours la même.  

Il n'y a pas une réponse unique à ce problème, des implémentations différentes de la nôtre pourraient très bien fonctionner.
Nous avons choisi d'associer plusieurs règles à la lettre F et d'en choisir une au hasard parmi ces dernières. Ainsi, à chaque detection de la lettre F dans la phrase, il est possible d'obtenir les traductions suivantes :

    F -> [+F][-F]
    ou
    F -> [+F]F[-F]
    ou
    F -> [-F]F[+F]

Nous avons également ajouté une chance d'ignorer une règle quand on détecte une lettre associée à une règle.  

Avec ces deux changements, on peut observer des formes très différentes de routes construites à partir de la même phrase de départ :

![Lsystem2](/screens/L-System2.png)
![Lsystem3](/screens/L-System3.png)
![Lsystem4](/screens/L-System4.png)

