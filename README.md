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
Un gars qui a codé en Python une génération procédurale de route en se basant sur a*

https://tel.archives-ouvertes.fr/tel-00841373/document \
These de Adrien Petavie: Generation de route

https://github.com/pboechat/roadgen/blob/master/%282001%29%20Procedural%20Modeling%20of%20Cities.pdf \
Road Network 

## Intro
------------------------------------------------
Nous allons utiliser un L-system pour générer plusieurs reseaux de routes qui prendraient source dans une route principale.

Un L-system se base sur 3 points : la valeur d'entrée, les règles qui lui sont appliquées et un nombre d'itération.

Dans la version que nous allons implémenter, l'entrée et la sortie seront des chaines de caractères, et les règles les différents changements que l'on peut y apporter. Il suffira alors de répéter l'opération en prenant la sortie de l'étape n-1 pour l'entrée de l'étape n.

La finalité sera une longue chaine de caractères que l'on pourra "traduire" en un tracé sur Unity. On souhaite placer de manière aléatoire sur une route principale différents points de départs de L-system. 
## Generation de la route principale
------------------------------------------------

Pour la generation de la route principale nous avons utiliser un algorithme d'A* qui relie un point de depart (Case vert) a un point d'arrive (Case rouge). Pour utiliser cette algorithme nous avons du mettre en place une grille de Case que l'algorithme pourra explorer.
#### Algorithme d'Astar
------------------------------------------------
L'algorithme utiliser est une simple algorithme d'A* qui cherche parmis les huits voisins a chaque iteration.
La map est separer en Node, un Node correpond a un noeud explorer par l'algoritme d'astar.

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

#### Generation Perlin Noise
------------------------------------------------
Nous voulons aussi que la route principale puisse prendre un aspect un peu plus hasardeux qu'une simple ligne rejoigant le point de depart et le point d'arrive.
Pour ca nous avons eu l'idee de mettre un bruit de perlin sur la grille precedement creer et de faire en sorte que l'algorithme d'A* esquive les zones ont le poids est important.
La generation de perlin noise a ete faite grace a la fonction PerlinNoise fournit par Unity.

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
GeneratePerlinNoise() -> Genere une valeur entre 0 et 1 pour chaque case du tableau (PerlinValue) qui fait la meme taille que la map

InitializeGrid() -> Initialise la grille en positionant chaque noeud, en leur assignant la valeur du bruit de perlin calculer precedement et en leur affactant une coleur lier par l'etat du noeud

FindPath(Node start, Node end) => Calcule le chemin entre le point de depart et le point d'arrive en prenant en conte le bruit de perlin generee et stocke le chemon calcule dans une variable (path).

### Rendu
------------------------------------------------
Affichage de la grille avec le point de depart(Case verte) et le point d'arrive (Case rouge)
![screen1](/screens/screen1.jpg)

Affichage du chemin en esquivant les endroits ou le poids est important
![screen2](/screens/screen3.jpg)

### Lissage de Courbe :

Lisser les routes avec les ensembles clothoides

Les clothoides sont des courbes dont la courbure varie lineairement en fonction d'une longueur d'arc.

Ces courbes sont egalement connus sont le nom spirale d'Euler

    cf https://fr.wikipedia.org/wiki/Clotho%C3%AFde
    cf https://mathcurve.com/courbes2d/cornu/cornu.shtml


