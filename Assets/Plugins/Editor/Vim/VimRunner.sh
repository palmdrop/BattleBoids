#!/bin/bash
#[Configuration]
TERM="st"

if [ ! -z `nvr --serverlist | grep Unity3d` ]; then
    nvr --servername Unity3d --remote-silent +$2 $1
else
    $TERM -e nvr --servername Unity3d --remote-silent +$2 $1
fi
