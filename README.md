# Monde Virtuel

Julien Perchero, Nuls Carron, Julien Martins


## Objectifs

- Route generee par A* + route subsidiaire en perpendiculaire
- Intersection / Smooth
- Topologie / Pont / Tunnel

## Partage de taches

Route genere par A* | Route L-System | Modelisation Route

Route genere par A* | Route L-System | Lissage de Courbe

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

### Generation de route

- Grille avec sur chaque cases un poids aleatoire
- Ne pas mettre un chiffre aleatoire met suivant une fonction

### Lissage de Courbe :

    Lisser les routes avec les ensembles clothoides

    Les clothoides sont des courbes dont la courbure varie lineairement en fonction d'une longueur d'arc.

    Ces courbes sont egalement connus sont le nom spirale d'Euler

        cf https://fr.wikipedia.org/wiki/Clotho%C3%AFde
        cf https://mathcurve.com/courbes2d/cornu/cornu.shtml

Generation de pont/tunnel (methode utilise) :
    - Convertir le plus court chemin en un ensemble discret d'arcs 
    - On segmente la trajectoire pour identifier les sections de routes / tunnels / ponts
    - On genere la route

### Documentation

FindPath(Node start, Node end) => Genere un chemin grace a l'algorithme Astar d'un point de depart a un point de sortie

La map est separer en Node, un Node correpond a un noeud explorer par l'algoritme d'astar.
Node:
    -> Index: Vector2
    -> WorldPos: Vector3
    -> Cout : int
    -> Heuristique: int
    -> Color: Color
    -> Pred : Node