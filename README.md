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

### Lissage de Courbe :

Lisser les routes avec les ensembles clothoides

Les clothoides sont des courbes dont la courbure varie lineairement en fonction d'une longueur d'arc.

Ces courbes sont egalement connus sont le nom spirale d'Euler

    cf https://fr.wikipedia.org/wiki/Clotho%C3%AFde
    cf https://mathcurve.com/courbes2d/cornu/cornu.shtml


### L-systems
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

