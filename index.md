---
layout: default
title: Home
---

## Posts

{% for post in site.posts %}
  * {{ post.date | date_to_long_string }} &raquo; [ {{ post.title }} ]({{ post.url }})
{% endfor %}
