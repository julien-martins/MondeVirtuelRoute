# L-system 

Nous allons utiliser un L-system pour générer plusieurs reseaux de routes qui prendraient source dans une route principale.

Un L-system se base sur 3 points : la valeur d'entrée, les règles qui lui sont appliquées et un nombre d'itération.

Dans la version que nous allons implémenter, l'entrée et la sortie seront des chaines de caractères, et les règles les différents changements que l'on peut y apporter. Il suffira alors de répéter l'opération en prenant la sortie de l'étape n-1 pour l'entrée de l'étape n.

La finalité sera une longue chaine de caractères que l'on pourra "traduire" en un tracé sur Unity. On souhaite placer de manière aléatoire sur une route principale différents points de départs de L-system. 